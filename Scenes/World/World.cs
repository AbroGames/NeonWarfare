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
        EventBus.Publish(new WorldReadyEvent(this));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        EventBus.Publish(new WorldProcessEvent(this, delta));
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        EventBus.Publish(new WorldPhysicsProcessEvent(this, delta));
    }
}