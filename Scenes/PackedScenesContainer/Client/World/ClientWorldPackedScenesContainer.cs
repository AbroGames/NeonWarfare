using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientWorldPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene SafeWorld { get; private set; }
    [Export] [NotNull] public PackedScene BattleWorld { get; private set; }
    
    [Export] [NotNull] public PackedScene FloatingLabel { get; private set; } //TODO перетащить в ScreenPackedSceneContainer
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}