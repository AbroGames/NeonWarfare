using System;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// The save files of a World that "Continue" follows.
/// After every file written <paramref name="setLastGameSave"/> gets its name, so "Continue" follows a "save as" too.
/// </summary>
public class LastGameUpdatingSaveFiles(ISaveFiles disk, Action<string> setLastGameSave) : ISaveFiles
{
    public bool IsAutoSaveEnabled() => disk.IsAutoSaveEnabled();

    public void SaveToDisk(byte[] data, string saveFileName)
    {
        disk.SaveToDisk(data, saveFileName);
        setLastGameSave(saveFileName);
    }
}
