using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public class GameSettingsService
{

    private const string GameSettingsPath = "user://game-settings.json";

    // Color is a Godot struct whose R/G/B/A are fields, which System.Text.Json skips by default.
    // Without the converter it writes only the computed properties (R8, H, OkHslH, ...), whose
    // setters then overwrite each other on read and produce a wrong color.
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerOptions.Default) { Converters = { new ColorJsonConverter() } };

    private GameSettings _settings;
    private string _temporalUid; 
    private string _temporalNick; 

    public void Init()
    {
        // Set default values (here, because we must do it only in Init method, not early)
        _settings = GameSettings.GetDefault();
        LoadSettings();
    }

    public GameSettings GetSettings()
    {
        return _settings with
        {
            PlayerUid = _temporalUid ?? _settings.PlayerUid,
            PlayerNick = _temporalNick ?? _settings.PlayerNick
        };
    }

    public void SetSettings(GameSettings gameSettings)
    {
        _settings = gameSettings;
        SaveSettings();
    }
    
    public void SetUidTemporarily(string uid)
    {
        _temporalUid = uid;
    }

    public void SetNickTemporarily(string nick)
    {
        _temporalNick = nick;
    }

    private void SaveSettings()
    {
        using var file = FileAccess.Open(GameSettingsPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetSettings(), JsonOptions);
        file.StoreString(json);
        file.Close();
    }

    private void LoadSettings()
    {
        if (!FileAccess.FileExists(GameSettingsPath))
        {
            SaveSettings();
            return;
        }
        
        using var file = FileAccess.Open(GameSettingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();
        
        // Keep the defaults from Init if the file turned out to be empty or malformed.
        _settings = JsonSerializer.Deserialize<GameSettings>(json, JsonOptions) ?? _settings;
    }
}