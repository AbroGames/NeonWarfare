using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts;

namespace NeonWarfare.GameTests.Worlds.Fixtures;

/// <summary>
/// Plays the disk: records every save file written, in order, and fails a write while <see cref="Failing"/> is set.
/// </summary>
public class RecordingSaveFiles : ISaveFiles
{
    public record Written(SaveFileName FileName, byte[] Data);

    public List<Written> Files { get; } = [];

    public bool Failing { get; set; }

    public void SaveToDisk(byte[] data, SaveFileName saveFileName)
    {
        if (Failing) throw new IOException($"writing '{saveFileName}' failed");

        Files.Add(new Written(saveFileName, data));
    }
}
