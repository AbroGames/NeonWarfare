using System;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The file a server World saves to from its start. A "save as" moves it to another file.
/// </summary>
public sealed record WorldSaveFile
{
    public WorldSaveFile(string fileName)
    {
        ArgumentNullException.ThrowIfNull(fileName);
        FileName = fileName;
    }

    public string FileName { get; }
}
