using System;
using System.Collections.Generic;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// The saves of the World, see Docs/Data-and-saves.md#saves.
/// </summary>
[Server]
public class SaveService(
    SaveWriter saveWriter, ServerTickClock clock, ISaveFiles saveFiles, WorldSaving saving, TimeProvider time)
{
    private readonly ILogger _log = LogFactory.GetForStatic<SaveService>();

    private readonly List<(SaveFileName FileName, Action Written, Action<Exception> Failed)> _requests = [];

    private readonly int _autoSaveIntervalTicks =
        TickRate.ToTicks(TimeSpan.FromMinutes(saving.AutoSaveIntervalMinutes).TotalSeconds);

    // Scheduled at the first tick, because the services are built before SaveLoader restores the tick counter.
    private long? _nextAutoSaveTick;

    /// <summary>
    /// The last file written. After a "save as" the autosave, the save on exit and the next load use the new file.
    /// </summary>
    public SaveFileName SaveFileName { get; private set; } = saving.FileName;

    /// <summary>
    /// Requests a save to <paramref name="saveFileName"/> at the end of the current tick.
    /// The callbacks run inside the tick and must not change the models.
    /// On a failure <see cref="SaveFileName"/> stays as it was.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="saveFileName"/> is invalid.</exception>
    public void RequestSave(SaveFileName saveFileName, Action written, Action<Exception> failed)
    {
        if (!saveFileName.IsValid)
        {
            throw new ArgumentException($"The save file name '{saveFileName}' is invalid.", nameof(saveFileName));
        }
        ArgumentNullException.ThrowIfNull(written);
        ArgumentNullException.ThrowIfNull(failed);

        _requests.Add((saveFileName, written, failed));
    }

    /// <summary>
    /// Writes one save for every request of the tick, and the autosave when an interval has passed since the first
    /// tick or the last write to <see cref="SaveFileName"/>.
    /// </summary>
    public void WriteRequested()
    {
        bool autoSave = false;
        if (_autoSaveIntervalTicks > 0)
        {
            _nextAutoSaveTick ??= clock.CurrentTick + _autoSaveIntervalTicks;
            autoSave = clock.CurrentTick >= _nextAutoSaveTick.Value;
        }
        if (_requests.Count == 0 && !autoSave) return;

        var requests = _requests.ToArray();
        _requests.Clear();
        // A due autosave waits a whole interval whatever happens to it, so a failing disk is not hit every tick.
        if (autoSave) ScheduleAutoSave();

        long started = time.GetTimestamp();
        byte[] save;
        try
        {
            save = saveWriter.Write();
        }
        catch (Exception e)
        {
            _log.Error(e, "The save failed");
            foreach ((_, _, Action<Exception> failed) in requests)
            {
                Run(() => failed(e));
            }
            return;
        }
        TimeSpan serializing = time.GetElapsedTime(started);

        // The autosave goes first, so a "save as" of the same tick still moves the save file.
        if (autoSave)
        {
            try
            {
                TimeSpan writing = WriteToDisk(save, SaveFileName);
                _log.Information(
                    "Autosaved to '{saveFileName}': serialize {serializeMs:0.00} ms, disk {diskMs:0.00} ms",
                    SaveFileName, serializing.TotalMilliseconds, writing.TotalMilliseconds);
            }
            catch (Exception e)
            {
                _log.Error(e, "The autosave to '{saveFileName}' failed", SaveFileName);
            }
        }

        foreach ((SaveFileName fileName, Action written, Action<Exception> failed) in requests)
        {
            try
            {
                WriteToDisk(save, fileName);
            }
            catch (Exception e)
            {
                _log.Error(e, "Writing the save '{saveFileName}' failed", fileName);
                Run(() => failed(e));
                continue;
            }
            SaveFileName = fileName;
            ScheduleAutoSave();
            Run(written);
        }
    }

    /// <summary>
    /// Never throws, because the World is leaving the tree and the teardown must go on whatever happens to the save.
    /// </summary>
    public void SaveOnExit()
    {
        try
        {
            // Without a tick the baselines are empty, and the save file, if any, already holds this state.
            if (!clock.Started)
            {
                _log.Information("No save on exit to '{saveFileName}': no tick has run, nothing new to save",
                    SaveFileName);
                return;
            }
            long started = time.GetTimestamp();
            byte[] save = saveWriter.Write();
            TimeSpan serializing = time.GetElapsedTime(started);
            TimeSpan writing = WriteToDisk(save, SaveFileName);
            _log.Information(
                "Saved on exit to '{saveFileName}': serialize {serializeMs:0.00} ms, disk {diskMs:0.00} ms",
                SaveFileName, serializing.TotalMilliseconds, writing.TotalMilliseconds);
        }
        catch (Exception e)
        {
            _log.Error(e, "The save on exit to '{saveFileName}' failed", SaveFileName);
        }
    }

    private void ScheduleAutoSave()
    {
        _nextAutoSaveTick = clock.CurrentTick + _autoSaveIntervalTicks;
    }

    private TimeSpan WriteToDisk(byte[] save, SaveFileName fileName)
    {
        long started = time.GetTimestamp();
        saveFiles.SaveToDisk(save, fileName);
        return time.GetElapsedTime(started);
    }

    private void Run(Action callback)
    {
        try
        {
            callback();
        }
        catch (Exception e)
        {
            _log.Error(e, "A save callback failed, the other callbacks still run");
        }
    }
}
