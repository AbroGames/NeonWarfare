using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public class GameSettingsService
{

    private const string GameSettingsPath = "user://game-settings.json";

    private GameSettings _settings;
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
            PlayerNick = _temporalNick ?? _settings.PlayerNick
        };
    }

    public void SetSettings(GameSettings gameSettings)
    {
        _settings = gameSettings;
        SaveSettings();
    }

    public void SetNickTemporarily(string nick)
    {
        _temporalNick = nick;
    }
    
    //public void PreserveSingleplayerGame(string saveName)
    //{
    //    Settings.FastResumeAvailable = GameSettings.ResumableGame.RunSingleplayer;
    //    Settings.LastSingleplayerSaveName = saveName;
    //    SaveSettings();
    //}
//
    //public void PreserveConnectionToServer(string host, int port)
    //{
    //    Settings.FastResumeAvailable = GameSettings.ResumableGame.ConnectToServer;
    //    Settings.LastConnectedHost = host;
    //    Settings.LastConnectedPort = port;
    //    SaveSettings();
    //}
//
    //public void PreserveServerCreation(int port, string saveName, bool asDedicated)
    //{
    //    Settings.FastResumeAvailable = GameSettings.ResumableGame.CreateServer;
    //    Settings.LastHostedIsDedicated = asDedicated;
    //    Settings.LastHostedPort = port;
    //    Settings.LastHostedSaveName = saveName;
    //    SaveSettings();
    //}

    private void SaveSettings()
    {
        using var file = FileAccess.Open(GameSettingsPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetSettings());
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
        
        GameSettings gameSettings = JsonSerializer.Deserialize<GameSettings>(json);
        SetSettings(gameSettings);
    }
}