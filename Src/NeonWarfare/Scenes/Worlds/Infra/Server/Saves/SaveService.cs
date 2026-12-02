using System;
using System.Collections.Generic;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// The save file of the World. <see cref="SaveFileName"/> is the last file written: after a "save as" the autosave and
/// the next load go to the new file. A requested save is the state at the end of the tick, the one the joining peers
/// get: it is served after the packets of the tick are written.
/// </summary>
[Server]
public class SaveService(SaveWriter saveWriter, ServerTickClock clock, ISaveFiles saveFiles)
{
    private const string InitializedError = "The save file name is already set.";
    private const string NotInitializedError = "The save file name is not set.";
    private const string WriteFailedLog = "The save failed";
    private const string DiskFailedLog = "Writing the save '{saveFileName}' failed";
    private const string CallbackFailedLog = "A save callback failed, the other callbacks still run";
    private const string AutoSaveDisabledLog = "No autosave: it is disabled";
    private const string NoTickLog = "No autosave to '{saveFileName}': no tick has run, nothing new to save";
    private const string AutoSaveFailedLog = "The autosave to '{saveFileName}' failed";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveService>();

    private readonly List<(string FileName, Action Written, Action<Exception> Failed)> _requests = [];

    public string SaveFileName { get; private set; }

    public void Init(string saveFileName)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        if (SaveFileName != null) throw new InvalidOperationException(InitializedError);

        SaveFileName = saveFileName;
    }

    /// <summary>
    /// A save to <paramref name="saveFileName"/> at the end of the current tick. The callbacks run inside the tick and
    /// must not change the models. On a failure the save file stays as it was.
    /// </summary>
    public void RequestSave(string saveFileName, Action written, Action<Exception> failed)
    {
        ArgumentNullException.ThrowIfNull(saveFileName);
        ArgumentNullException.ThrowIfNull(written);
        ArgumentNullException.ThrowIfNull(failed);

        _requests.Add((saveFileName, written, failed));
    }

    // Calls from ServerTickLoop. One save for every request of the tick
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
            _log.Error(e, WriteFailedLog);
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
                _log.Error(e, DiskFailedLog, fileName);
                Run(() => failed(e));
                continue;
            }
            SaveFileName = fileName;
            Run(written);
        }
    }

    /// <summary>
    /// Never throws: the World is leaving the tree, and the teardown must go on whatever happens to the save.
    /// </summary>
    public void SaveOnExit()
    {
        try
        {
            if (!saveFiles.IsAutoSaveEnabled())
            {
                _log.Information(AutoSaveDisabledLog);
                return;
            }
            // Its file, if any, already holds this state, and the baselines hold none of it yet
            if (!clock.Started)
            {
                _log.Information(NoTickLog, SaveFileName);
                return;
            }
            if (SaveFileName == null) throw new InvalidOperationException(NotInitializedError);

            saveFiles.SaveToDisk(saveWriter.Write(), SaveFileName);
        }
        catch (Exception e)
        {
            _log.Error(e, AutoSaveFailedLog, SaveFileName);
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
            _log.Error(e, CallbackFailedLog);
        }
    }
}
