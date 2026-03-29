using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public class PlayerSettingsService : IPlayerSettingsService
{

    private readonly string _playerSettingsPath = "user://player-settings.json";
    
    private string _nick;
    private Color _color;
    private string _language;

    private string _temporalNick; 

    public void Init()
    {
        // Set default values (here, because we must do it only in Init method, not early)
        _nick = "Player";
        _color = new Color(1, 1, 1);
        _language = Services.I18N.GetUserOsLocaleInfoOrDefault().Code;
        
        Load();
    }

    public PlayerSettings GetPlayerSettings()
    {
        return new PlayerSettings(_temporalNick ?? _nick, _color, _language);
    }

    public void SetPlayerSettings(PlayerSettings playerSettings)
    {
        _nick ??= playerSettings.Nick;
        _color = playerSettings.Color;
        _language ??= playerSettings.Language;
        
        Save();
    }

    public void SetNick(string nick)
    {
        _nick = nick;
        Save();
    }

    public void SetColor(Color color)
    {
        _color = color;
        Save();
    }

    public void SetLanguage(string language)
    {
        _language = language;
        Save();
    }

    public void SetNickTemporarily(string nick)
    {
        _temporalNick = nick;
    }

    private void Save()
    {
        using var file = FileAccess.Open(_playerSettingsPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetPlayerSettings());
        file.StoreString(json);
        file.Close();
    }

    private void Load()
    {
        if (!FileAccess.FileExists(_playerSettingsPath))
        {
            Save();
            return;
        }
        
        using var file = FileAccess.Open(_playerSettingsPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();
        
        PlayerSettings playerSettings = JsonSerializer.Deserialize<PlayerSettings>(json);
        SetPlayerSettings(playerSettings);
    }
}