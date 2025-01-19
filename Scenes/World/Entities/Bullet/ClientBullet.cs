using Godot;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities;

public partial class ClientBullet : Node2D
{
    public const float Speed = ServerBullet.Speed;
    
    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}