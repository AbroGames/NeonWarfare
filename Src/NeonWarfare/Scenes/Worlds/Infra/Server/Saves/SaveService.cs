using System;
using System.Collections.Generic;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// The saves of the World, see Docs/Data-and-saves.md#saves.
/// </summary>
[Server]
public class SaveService(SaveWriter saveWriter, ServerTickClock clock, ISaveFiles saveFiles, WorldSaveFile saveFile)
{
    private readonly ILogger _log = LogFactory.GetForStatic<SaveService>();

    private readonly List<(string FileName, Action Written, Action<Exception> Failed)> _requests = [];

    /// <summary>The last file written. After a "save as" the autosave and the next load use the new file.</summary>
    public string SaveFileName { get; private set; } = saveFile.FileName;

    /// <summary>
    /// Requests a save to <paramref name="saveFileName"/> at the end of the current tick.
    /// The callbacks run inside the tick and must not change the models.
    /// On a failure <see cref="SaveFileName"/> stays as it was.
    /// </summary>
    public void RequestSave(string saveFileName, Action written, Action<Exception> failed)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        ArgumentNullException.ThrowIfNull(written);
        ArgumentNullException.ThrowIfNull(failed);

        _requests.Add((saveFileName, written, failed));
    }

    /// <summary>Writes one save for every request of the tick.</summary>
    public void WriteRequested()
    {
        if (_requests.Count == 0) return;

        var requests = _requests.ToArray();
        _requests.Clear();

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

        foreach ((string fileName, Action written, Action<Exception> failed) in requests)
        {
            try
            {
                saveFiles.SaveToDisk(save, fileName);
            }
            catch (Exception e)
            {
                _log.Error(e, "Writing the save '{saveFileName}' failed", fileName);
                Run(() => failed(e));
                continue;
            }
            SaveFileName = fileName;
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
            if (!saveFiles.IsAutoSaveEnabled())
            {
                _log.Information("No autosave: it is disabled");
                return;
            }
            // Without a tick the baselines are empty, and the save file, if any, already holds this state.
            if (!clock.Started)
            {
                _log.Information("No autosave to '{saveFileName}': no tick has run, nothing new to save", SaveFileName);
                return;
            }
            saveFiles.SaveToDisk(saveWriter.Write(), SaveFileName);
        }
        catch (Exception e)
        {
            _log.Error(e, "The autosave to '{saveFileName}' failed", SaveFileName);
        }
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
