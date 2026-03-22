using Godot;
using NeonWarfare.Scenes.Game.Starters;

namespace Kludgeful.Main.SettingsSystem;

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
    public string PlayerName { get; set; } = "No Name";
    
    [Hint("Color of the player character")]
    public Color PlayerColor { get; set; } = Colors.GreenYellow;

    [Hide] public ResumableGame FastResumeAvailable { get; set; } = ResumableGame.None;
    
    [Hide] public string LastConnectedHost { get; set; } = BaseGameStarter.DefaultHost;
    [Hide] public int LastConnectedPort { get; set; } = BaseGameStarter.DefaultPort;
    
    [Hide] public int LastHostedPort { get; set; } = BaseGameStarter.DefaultPort;
    [Hide] public bool LastHostedIsDedicated { get; set; } = false;
    [Hide] public string LastHostedSaveName { get; set; }
}