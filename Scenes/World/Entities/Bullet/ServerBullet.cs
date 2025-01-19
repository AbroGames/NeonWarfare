using Godot;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities;

public partial class ServerBullet : Node2D
{
    public const float Speed = 2000;
    public const float Ttl = 3;
    private ManualCooldown ttlCooldown = new ManualCooldown(Ttl);

    public override void _Ready()
    {
        base._Ready();
        ttlCooldown.ActionWhenReady += () =>
        {
            QueueFree();
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        ttlCooldown.Update(delta);
        
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}