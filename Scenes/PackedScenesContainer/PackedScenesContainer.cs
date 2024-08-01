using Godot;
using KludgeBox;
using NeonWarfare.Client;

namespace NeonWarfare;

public partial class PackedScenesContainer : Node
{
    [Export] [NotNull] public ClientPackedScenesContainer Client { get; private set; }
    [Export] [NotNull] public CommonPackedScenesContainer Common { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}