using Godot;
using KludgeBox;

namespace NeonWarfare;

public abstract partial class World : Node2D
{
    [Export] [NotNull] public Floor Floor { get; set; } //TODO to client, or del this class
    
    public Hud Hud { get; set; } //TODO to client, or del this class
    public Player Player;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}