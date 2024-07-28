using Godot;
using KludgeBox;

namespace NeonWarfare.Client;

public partial class CommonPackedScenesContainer : Node
{
    [Export] [NotNull] public CommonWorldPackedScenesContainer World { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}