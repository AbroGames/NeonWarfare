using System;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Starters;

/// <summary>
/// The save files of a World that "Continue" follows.
/// After every file written <paramref name="setLastGameSave"/> gets its name, so "Continue" follows a "save as" too.
/// <paramref name="setLastGameSave"/> must not throw, because the save file is already written and an exception would
/// report that save as failed.
/// </summary>
public class LastGameUpdatingSaveFiles(ISaveFiles disk, Action<SaveFileName> setLastGameSave) : ISaveFiles
{
    public void SaveToDisk(byte[] data, SaveFileName saveFileName)
    {
        disk.SaveToDisk(data, saveFileName);
        setLastGameSave(saveFileName);
    }
}
