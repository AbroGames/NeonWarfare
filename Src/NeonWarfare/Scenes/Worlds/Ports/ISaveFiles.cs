
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>The save files on disk and the autosave setting. They belong to the process, not to a World.</summary>
public interface ISaveFiles
{
    bool IsAutoSaveEnabled();

    void SaveToDisk(byte[] data, string saveFileName);
}
