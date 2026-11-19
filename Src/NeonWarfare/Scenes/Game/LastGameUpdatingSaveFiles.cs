using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// The save files of a World that "Continue" follows: after every file written, <paramref name="setLastGameSave"/>
/// gets its name, which a "save as" changes.
/// </summary>
public class LastGameUpdatingSaveFiles(ISaveFiles disk, Action<string> setLastGameSave) : ISaveFiles
{
    private const string LastGameFailedLog = "Pointing the last game at the save '{saveFileName}' failed";

    private readonly ILogger _log = LogFactory.GetForStatic<LastGameUpdatingSaveFiles>();

    public bool IsAutoSaveEnabled() => disk.IsAutoSaveEnabled();

    public void SaveToDisk(byte[] data, string saveFileName)
    {
        disk.SaveToDisk(data, saveFileName);
        // The file is written: the save must not be reported as failed
        try
        {
            setLastGameSave(saveFileName);
        }
        catch (Exception e)
        {
            _log.Error(e, LastGameFailedLog, saveFileName);
        }
    }
}
