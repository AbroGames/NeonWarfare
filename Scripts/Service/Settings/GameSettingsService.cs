using Godot;
using NeonWarfare.Scenes.NeonTemp.UI.Menu.SettingsSystem;

namespace NeonWarfare.Scripts.Service.Settings;

public class GameSettingsService : IPlayerSettingsService
{
    private readonly string _gameSettingsPath = "user://game-settings.json";
    public GameSettings Settings { get; private set; }
    
    private string _temporalNick = null;

    public void PreserveSingleplayerGame(string saveName)
    {
        Settings.FastResumeAvailable = ResumableGame.RunSingleplayer;
        Settings.LastSingleplayerSaveName = saveName;
        SaveSettings();
    }

    public void PreserveConnectionToServer(string host, int port)
    {
        Settings.FastResumeAvailable = ResumableGame.ConnectToServer;
        Settings.LastConnectedHost = host;
        Settings.LastConnectedPort = port;
        SaveSettings();
    }

    public void PreserveServerCreation(int port, string saveName, bool asDedicated)
    {
        Settings.FastResumeAvailable = ResumableGame.CreateServer;
        Settings.LastHostedIsDedicated = asDedicated;
        Settings.LastHostedPort = port;
        Settings.LastHostedSaveName = saveName;
        SaveSettings();
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
            Settings.Validate();
        }
    }

    public PlayerSettings GetPlayerSettings()
    {
        return new PlayerSettings(_temporalNick ?? Settings.PlayerName, Settings.PlayerColor, Settings.GameLocale); //TODO GameLocale -- это настройка игрока разве? Кажется игры
    }

    public void SetPlayerSettings(PlayerSettings playerSettings)
    {
        Settings.PlayerColor = playerSettings.Color;
        Settings.PlayerName = playerSettings.Nick; //TODO Здесь не хватает GameLocale
        SaveSettings();
    }

    public void SetNickTemporarily(string nick)
    {
        _temporalNick = nick;
    }
}