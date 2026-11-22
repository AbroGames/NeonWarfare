using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// The hybrid player of variant B: a CharacterBody2D among rigid bots. The solver treats it as kinematic,
/// so the crowd never shoves it, while MoveAndSlide still stops it at the bots when blocking is on.
/// </summary>
public partial class CharacterBodyPlayer : CharacterBody2D, IBenchPlayerBody
{
    private static readonly Color BodyColor = new(1.0f, 0.55f, 0.2f);

    private float _facing;

    public CharacterBodyPlayer()
    {
        CollisionLayer = BenchLayers.Player;
        CollisionMask = BenchLayers.Walls;
        MotionMode = MotionModeEnum.Floating;

        AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = BenchSpecs.PlayerRadius },
        });
    }

    public Node Node => this;

    public Vector2 Position => GlobalPosition;

    public void ApplyVelocity(Vector2 velocity)
    {
        Velocity = velocity;
        MoveAndSlide();
    }

    public void SetFacing(float angleRadians)
    {
        _facing = angleRadians;
    }

    public void SetBlocksEnemies(bool enabled)
    {
        CollisionMask = enabled
            ? BenchLayers.Walls | BenchLayers.Enemies
            : BenchLayers.Walls;
    }

    public void PlaceAt(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
        Velocity = Vector2.Zero;
        ResetPhysicsInterpolation();
    }

    public void SetContinuousCollision(bool enabled)
    {
        // A no-op: MoveAndSlide sweeps the shape along the whole motion.
    }

    public override void _Process(double delta)
    {
        // The facing line is drawn in local space and the angle changes every frame.
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, BenchSpecs.PlayerRadius, BodyColor);
        DrawLine(Vector2.Zero, Vector2.FromAngle(_facing) * (BenchSpecs.PlayerRadius + 10f), BodyColor, 2f);
    }
}
