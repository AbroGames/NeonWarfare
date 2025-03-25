using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scenes.World.Entities.Actions;

public partial class ClientShotAction : Node2D
{
    
    [Export] [NotNull] public Sprite2D Sprite { get; private set; }

    public float Speed { get; set; } //TODO Init from packet/skillInfo

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }
    
    public override void _PhysicsProcess(double delta)
    {
        Position += Vector2.FromAngle(Rotation - Mathf.DegToRad(90)) * Speed * (float) delta;
    }
}