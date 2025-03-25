using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Actions;

public partial class ServerShotAction : Node2D
{
    
    [Export] [NotNull] public Area2D HitBox { get; private set; }

    private const float Ttl = 3; //sec
    
    public float Speed = 2000; //Init from skill
    private ManualCooldown _destroyCooldown;

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        
        _destroyCooldown = new ManualCooldown(Ttl, false, true, QueueFree);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        _destroyCooldown.Update(delta);
        
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}