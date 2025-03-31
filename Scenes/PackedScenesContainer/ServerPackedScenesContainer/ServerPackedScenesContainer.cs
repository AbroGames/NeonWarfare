using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.PackedScenesContainer.ServerPackedScenesContainer;

public partial class ServerPackedScenesContainer : Node
{
    
    /*
     * WORLD
     */
    [ExportGroup("World")]
    [Export] [NotNull] public PackedScene SafeWorld { get; private set; }
    [Export] [NotNull] public PackedScene BattleWorld { get; private set; }
    
    [Export] [NotNull] public PackedScene Player { get; private set; }
    
    [Export] [NotNull] public PackedScene Wall { get; private set; }
    
    /*
     * ENEMY
     */
    [ExportGroup("Enemy")]
    [Export] [NotNull] public PackedScene ZergEnemy { get; private set; }
    [Export] [NotNull] public PackedScene ShooterEnemy { get; private set; }
    [Export] [NotNull] public PackedScene TurtleEnemy { get; private set; }
    
    /*
     * Action
     */
    [ExportGroup("Action")]
    [Export] [NotNull] public PackedScene ShotAction { get; private set; }
    [Export] [NotNull] public PackedScene HealShotAction { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}
