using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class World : Node2D
{
    [Export] [NotNull] public Floor Floor { get; set; }
    
    public Hud Hud { get; set; }
    public Player Player;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}