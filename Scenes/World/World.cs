using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public abstract partial class World : Node2D
{
    [Export] [NotNull] public Floor Floor { get; set; }
    public Player Player;
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
}