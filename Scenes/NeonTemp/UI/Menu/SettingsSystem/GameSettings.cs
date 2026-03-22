using Godot;

namespace Kludgeful.Main.SettingsSystem;

public partial class GameSettings : GameSettingsBase
{
    [Hint("Name of the player in the multiplayer")]
    public string PlayerName { get; set; } = "No Name";
    
    [Hint("Color of the player character")]
    public Color PlayerColor { get; set; } = Colors.GreenYellow;

    [Hide] public string LastConnectedHost { get; set; } = "127.0.0.1";
}