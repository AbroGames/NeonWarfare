using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class CommonWorldPackedScenesContainer : Node
{
    
    [Export] [NotNull] public PackedScene Player { get; private set; }
    [Export] [NotNull] public PackedScene Ally { get; private set; }
    [Export] [NotNull] public PackedScene Enemy { get; private set; }
    [Export] [NotNull] public PackedScene Boss { get; private set; }
    
    [Export] [NotNull] public PackedScene Bullet { get; private set; }
    [Export] [NotNull] public PackedScene SolarBeam { get; private set; }
    [Export] [NotNull] public PackedScene Beam { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}