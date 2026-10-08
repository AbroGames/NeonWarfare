using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.CharacterBodyVariant;

/// <summary>
/// The player body of variant A. Arcade movement: the prescribed velocity goes straight into the body
/// every tick; nothing is ever integrated from forces.
/// </summary>
public partial class CharacterBodyBenchPlayer : CharacterBody2D, IBenchPlayerBody
{
    private static readonly Color BodyColor = new(1.0f, 0.85f, 0.2f);

    private float _facing;

    public CharacterBodyBenchPlayer()
    {
        CollisionLayer = BenchLayers.Player;
        CollisionMask = BenchLayers.Walls;
        // Floating because the arena is top-down; MaxSlides, SafeMargin and PlatformOnLeave stay at the
        // engine defaults (4 / 0.08 px / Additive): the arena has no platforms, and four slides are
        // enough for every corner this arena can squeeze the player into.
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
        // A no-op: MoveAndSlide sweeps the shape along the whole motion, so the body cannot tunnel the
        // way a discretely integrated one can.
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
