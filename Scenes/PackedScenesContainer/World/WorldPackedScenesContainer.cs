using Godot;
using KludgeBox;

public partial class WorldPackedScenesContainer : Node
{
    [Export] [NotNull] public PackedScene Player { get; private set; }
    [Export] [NotNull] public PackedScene Ally { get; private set; }
    [Export] [NotNull] public PackedScene Enemy { get; private set; }
    [Export] [NotNull] public PackedScene Boss { get; private set; }
    
    [Export] [NotNull] public PackedScene Bullet { get; private set; }
    [Export] [NotNull] public PackedScene SolarBeam { get; private set; }
    [Export] [NotNull] public PackedScene Beam { get; private set; }
    
    [Export] [NotNull] public PackedScene XpOrb { get; private set; }
    [Export] [NotNull] public PackedScene FloatingLabel { get; private set; }
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}