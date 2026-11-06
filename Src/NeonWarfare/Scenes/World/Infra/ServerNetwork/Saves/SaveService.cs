using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using Serilog;

namespace NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;

/// <summary>
/// The save file of the World. <see cref="SaveFileName"/> is the last file written: after a "save as" the autosave and
/// the next load go to the new file.
/// </summary>
[ServerNetwork]
public class SaveService(SaveWriter saveWriter, ServerTickLoop tickLoop, ISaveFiles saveFiles)
{
    private const string InitializedError = "The save file name is already set.";
    private const string NotInitializedError = "The save file name is not set.";
    private const string DiskFailedLog = "Writing the save '{saveFileName}' failed";
    private const string AutoSaveDisabledLog = "No autosave: it is disabled";
    private const string NoTickLog = "No autosave to '{saveFileName}': no tick has run, nothing new to save";
    private const string AutoSaveFailedLog = "The autosave to '{saveFileName}' failed";
    private const string SavedHandlerFailedLog = "A handler of the save to '{saveFileName}' failed";

    private readonly ILogger _log = LogFactory.GetForStatic<SaveService>();

    public string SaveFileName { get; private set; }

    /// <summary>
    /// After every save file written, with its name.
    /// </summary>
    public event Action<string> SavedEvent;

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

        saveWriter.RequestSave(save =>
        {
            try
            {
                saveFiles.SaveToDisk(save, saveFileName);
            }
            catch (Exception e)
            {
                _log.Error(e, DiskFailedLog, saveFileName);
                failed(e);
                return;
            }
            Saved(saveFileName);
            written();
        }, failed);
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
            if (!tickLoop.Started)
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
            return;
        }
        Saved(SaveFileName);
    }

    // The file is written whatever its handlers do, so the name follows it
    private void Saved(string saveFileName)
    {
        SaveFileName = saveFileName;
        try
        {
            SavedEvent?.Invoke(saveFileName);
        }
        catch (Exception e)
        {
            _log.Error(e, SavedHandlerFailedLog, saveFileName);
        }
    }
}
