namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>The save files on disk. They belong to the process, not to a World.</summary>
public interface ISaveFiles
{
    void SaveToDisk(byte[] data, SaveFileName saveFileName);
}
