using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ScreenPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene MainMenuPackedScene { get; private set; }
    
    [Export] [NotNull] public PackedScene MainMenu { get; private set; }
    [Export] [NotNull] public PackedScene SettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene PlayerSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene GraphicSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene SoundSettingsMenu { get; private set; }
    [Export] [NotNull] public PackedScene ConnectToServerMenu { get; private set; }
    [Export] [NotNull] public PackedScene CreateServerMenu { get; private set; }
    [Export] [NotNull] public PackedScene WaitingConnectionScreen { get; private set; }
    [Export] [NotNull] public PackedScene WaitingForBattleEndScreen { get; private set; }
    
    [Export] [NotNull] public PackedScene SafeHud { get; private set; }
    [Export] [NotNull] public PackedScene BattleHud { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}