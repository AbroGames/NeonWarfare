using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ClientGameMainScenesPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene BattleWorld { get; private set; }
    [Export] [NotNull] public PackedScene SafeWorld { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}