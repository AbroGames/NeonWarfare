using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;

namespace NeonWarfare.GameTests.World.Fixtures;

/// <summary>
/// Plays the disk: records every save file written, in order, and fails a write while <see cref="Failing"/> is set.
/// </summary>
public class RecordingSaveFiles : ISaveFiles
{
    public record Written(string FileName, byte[] Data);

    public List<Written> Files { get; } = [];

    public bool AutoSaveEnabled { get; set; } = true;

    public bool Failing { get; set; }

    public bool IsAutoSaveEnabled() => AutoSaveEnabled;

    public void SaveToDisk(byte[] data, string saveFileName)
    {
        if (Failing) throw new IOException($"writing '{saveFileName}' failed");

        Files.Add(new Written(saveFileName, data));
    }
}
