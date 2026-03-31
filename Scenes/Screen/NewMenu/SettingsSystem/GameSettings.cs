using System;
using Godot;

namespace NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

public enum ResumableGame
{
    None,
    RunSingleplayer,
    CreateServer,
    ConnectToServer,
}

public partial class GameSettings
{
    [Name("SETTING_MENU__NICK")]
    [Hint("SETTING_MENU__NICK_HINT")]
    public string PlayerName { get; set; } = Consts.DefaultPlayerName;
    
    [Name("SETTING_MENU__COLOR")]
    [Hint("SETTING_MENU__COLOR_HINT")]
    public Color PlayerColor { get; set; } = Colors.GreenYellow;


    [Hide] public string GameLocale { get; set; } = Services.I18N.GetUserOsLocaleInfoOrDefault().Code;
    [Hide] public ResumableGame FastResumeAvailable { get; set; } = ResumableGame.None;
    
    [Hide] public string LastSingleplayerSaveName { get; set; }

    [Hide] public string LastConnectedHost { get; set; } = String.Empty;
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