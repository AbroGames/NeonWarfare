
namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The save files on disk and the autosave setting, owned by the process rather than by a World.
/// </summary>
public interface ISaveFiles
{
    bool IsAutoSaveEnabled();

    void SaveToDisk(byte[] data, string saveFileName);
}
