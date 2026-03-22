using Godot;
using Kludgeful.Main.SettingsSystem;

namespace NeonWarfare.Scripts.Service.Settings;

public class GameSettingsService : IPlayerSettingsService
{
    private readonly string _gameSettingsPath = "user://game-settings.json";
    public GameSettings Settings { get; private set; }
    public bool FastResumeEnabled { get; private set; } = true;
    
    private string _temporalNick = null;

    public void PreserveSingleplayerGame()
    {
        if (!FastResumeEnabled) return;
        Settings.FastResumeAvailable = ResumableGame.RunSingleplayer;
    }

    public void PreserveConnectionToServer(string host, int port)
    {
        if (!FastResumeEnabled) return;
        Settings.FastResumeAvailable = ResumableGame.ConnectToServer;
        Settings.LastConnectedHost = host;
        Settings.LastConnectedPort = port;
    }

    public void PreserveServerCreation(int port, string saveName, bool asDedicated)
    {
        if (!FastResumeEnabled) return;
        Settings.FastResumeAvailable = ResumableGame.CreateServer;
        Settings.LastHostedIsDedicated = asDedicated;
        Settings.LastHostedPort = port;
        Settings.LastHostedSaveName = saveName;
    }
    
    public void Init()
    {
        LoadSettings();
    }
    
    public void SaveSettings()
    {
        using var file = FileAccess.Open(_gameSettingsPath, FileAccess.ModeFlags.Write);
        var text = Settings.Serialize();
        file.StoreString(text);
    }

    private void LoadSettings()
    {
        if (!FileAccess.FileExists(_gameSettingsPath))
        {
            Settings = new GameSettings();
            SaveSettings();
        }
        else
        {
            using var file = FileAccess.Open(_gameSettingsPath, FileAccess.ModeFlags.Read);
            var text = file.GetAsText();
            Settings = GameSettingsBase.Deserialize(text);
        }
    }

    public PlayerSettings GetPlayerSettings()
    {
        return new PlayerSettings(_temporalNick ?? Settings.PlayerName, Settings.PlayerColor);
    }

    public void SetPlayerSettings(PlayerSettings playerSettings)
    {
        Settings.PlayerColor = playerSettings.Color;
        Settings.PlayerName = playerSettings.Nick;
        SaveSettings();
    }

    public void SetNickTemporarily(string nick)
    {
        _temporalNick = nick;
    }
}