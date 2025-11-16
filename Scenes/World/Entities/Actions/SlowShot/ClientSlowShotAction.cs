using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Entities.Actions.SlowShot;

public partial class ClientSlowShotAction : Node2D
{

    public float Speed { get; private set; } 

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public void Init(Vector2 startPosition, float rotation)
    {
        Position = startPosition;
        Rotation = rotation;
    }

    public void InitStats(float speed)
    {
        Speed = speed;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}