using System;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The file a server World saves to from its start. A "save as" moves it to another file.
/// </summary>
public sealed record WorldSaveFile
{
    public WorldSaveFile(string fileName, bool autoSave)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        FileName = fileName;
        AutoSave = autoSave;
    }

    public string FileName { get; }

    public bool AutoSave { get; }
}
