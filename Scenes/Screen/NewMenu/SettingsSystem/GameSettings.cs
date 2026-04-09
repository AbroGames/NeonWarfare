using Godot;

namespace NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

public enum ResumableGame
{
    None,
    RunSingleplayer,
    CreateServer,
    ConnectToServer,
}

public partial class GameSettings : GameSettingsBase
{
    [Hint("Name of the player in the multiplayer")]
    public string PlayerName { get; set; } = Consts.DefaultPlayerName;
    
    [Hint("Color of the player character")]
    public Color PlayerColor { get; set; } = Colors.GreenYellow;


    [Hide] public string GameLocale { get; set; } = Services.I18N.GetUserOsLocaleInfoOrDefault().Code;
    [Hide] public ResumableGame FastResumeAvailable { get; set; } = ResumableGame.None;
    
    [Hide] public string LastSingleplayerSaveName { get; set; }
    
    [Hide] public string LastConnectedHost { get; set; }
    [Hide] public int LastConnectedPort { get; set; } = Consts.DefaultPort;
    
    [Hide] public int LastHostedPort { get; set; } = Consts.DefaultPort;
    [Hide] public bool LastHostedIsDedicated { get; set; }
    [Hide] public string LastHostedSaveName { get; set; }

    public void Validate()
    {
        GameLocale ??= Services.I18N.GetUserOsLocaleInfoOrDefault().Code;
        PlayerName ??= Consts.DefaultPlayerName;
    }
}