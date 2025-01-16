using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.PackedScenesContainer.ServerPackedScenesContainer;

public partial class ServerPackedScenesContainer : Node
{
    
    /*
     * WORLD
     */
    [ExportGroup("World")]
    [Export] [NotNull] public PackedScene Player { get; private set; }
    [Export] [NotNull] public PackedScene Enemy { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}
