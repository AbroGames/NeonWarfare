using System.Text.Json;
using Godot;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scripts.GlobalServices.ResumableGame;

public class ResumableGameService
{
    
    private const string LastGameSettingPath = "user://resume-game.json";

    private readonly ILogger _log = LogFactory.GetForStatic<ResumableGameService>();

    private ResumableGame _lastGame;

    public void Init()
    {
        _lastGame = ResumableGame.GetNone();
        LoadSettings();
    }

    // A child dedicated server rewrites the file after a "save as", so the copy read at startup goes stale
    public ResumableGame GetLastGame()
    {
        LoadSettings();
        return _lastGame;
    }
    
    public void SetLastGame(ResumableGame lastGame)
    {
        _lastGame = lastGame;
        SaveSettings();
    }
    
    public void StartLastGame()
    {
        StartResumableGame(GetLastGame());
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
    
    // Never throws: "Continue" is a convenience, and a save whose file is written must not be reported as failed
    private void SaveSettings()
    {
        using var file = FileAccess.Open(LastGameSettingPath, FileAccess.ModeFlags.Write);
        if (file == null)
        {
            _log.Error("Failed to open '{path}': {error}", LastGameSettingPath, FileAccess.GetOpenError());
            return;
        }
        string json = JsonSerializer.Serialize(_lastGame);
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