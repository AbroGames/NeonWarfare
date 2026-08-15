using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public class DedicatedServerSettingsService
{
    private const string DedicatedServerSettingsPath = "user://dedicated-server-settings.json";

    private DedicatedServerSettings _settings;

    public void Init()
    {
        // Set default values (here, because we must do it only in Init method, not early)
        _settings = DedicatedServerSettings.GetDefault();
        LoadSettings();
    }

    public DedicatedServerSettings GetSettings()
    {
        return _settings;
    }

    public void SetSettings(DedicatedServerSettings settings)
    {
        _settings = settings;
        SaveSettings();
    }

    private void SaveSettings()
    {
        using var file = FileAccess.Open(DedicatedServerSettingsPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetSettings());
        file.StoreString(json);
        file.Close();
    }

    private void LoadSettings()
    {
        if (!FileAccess.FileExists(DedicatedServerSettingsPath))
        {
            SaveSettings();
            return;
        }

        using var file = FileAccess.Open(DedicatedServerSettingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();

        _settings = JsonSerializer.Deserialize<DedicatedServerSettings>(json);
    }
}
