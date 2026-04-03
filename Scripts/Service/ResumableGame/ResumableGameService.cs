using System.Text.Json;
using Godot;

namespace NeonWarfare.Scripts.Service.ResumableGame;

public class ResumableGameService
{
    
    private const string LastGameSettingPath = "user://resume-game.json";

    private ResumableGame _lastGame;

    public void Init()
    {
        _lastGame = ResumableGame.GetNone();
        LoadSettings();
    }

    public ResumableGame GetLastGame()
    {
        return _lastGame;
    }
    
    public void SetLastGame(ResumableGame lastGame)
    {
        _lastGame = lastGame;
        SaveSettings();
    }
    
    public void StartLastGame()
    {
        StartResumableGame(_lastGame);
    }
    
    public void StartResumableGame(ResumableGame game)
    {
        switch (game.Type)
        { 
            case ResumableGame.ResumableType.RunSingleplayer: 
                Services.MainScene.StartSingleplayerGame(game.SaveName);
                break;
            case ResumableGame.ResumableType.ConnectToServer: 
                Services.MainScene.ConnectToMultiplayerGame(game.Host, game.Port);
                break;
            case ResumableGame.ResumableType.CreateServer: 
                Services.MainScene.HostMultiplayerGameAsClient(game.SaveName, game.Port, game.IsDedicated!.Value);
                break;
        }
    }
    
    private void SaveSettings()
    {
        using var file = FileAccess.Open(LastGameSettingPath, FileAccess.ModeFlags.Write);
        string json = JsonSerializer.Serialize(GetLastGame());
        file.StoreString(json);
        file.Close();
    }

    private void LoadSettings()
    {
        if (!FileAccess.FileExists(LastGameSettingPath))
        {
            SaveSettings();
            return;
        }
        
        using var file = FileAccess.Open(LastGameSettingPath, FileAccess.ModeFlags.Read);
        string json = file.GetAsText();
        file.Close();

        _lastGame = JsonSerializer.Deserialize<ResumableGame>(json);
    }
}