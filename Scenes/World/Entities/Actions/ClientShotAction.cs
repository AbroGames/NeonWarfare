using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Entities.Actions;

public partial class ClientShotAction : Node2D
{
    
    [Export] [NotNull] public Sprite2D Sprite { get; private set; }

    public float Speed { get; private set; } 

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public void Init(Vector2 startPosition, float rotation) //TODO Init from packet/skillInfo
    {
        Position = startPosition;
        Rotation = rotation;
    }

    public void InitStats(float speed, Color color) //TODO Init from packet/skillInfo
    {
        Speed = speed;
        Sprite.Modulate = color;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}