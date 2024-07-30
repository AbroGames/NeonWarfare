using Godot;
using KludgeBox;

namespace NeonWarfare.Client;

public partial class ClientPackedScenesContainer : Node
{
    [Export] [NotNull] public ClientEffectsPackedScenesContainer Effects { get; private set; }
    [Export] [NotNull] public ClientGameMainScenesPackedScenesContainer GameMainScenes { get; private set; }
    [Export] [NotNull] public ClientScreensPackedScenesContainer Screens { get; private set; }
    [Export] [NotNull] public ClientWorldPackedScenesContainer World { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}