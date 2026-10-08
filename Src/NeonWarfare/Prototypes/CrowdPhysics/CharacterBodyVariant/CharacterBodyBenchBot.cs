using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.CharacterBodyVariant;

/// <summary>
/// The bot body of variant A. The prescribed velocity goes into MoveAndSlide every tick, so the
/// displacement it reports includes the slides — that is what the jitter metric is supposed to see.
/// </summary>
public partial class CharacterBodyBenchBot : CharacterBody2D, IBenchBotBody
{
    private static readonly Color BodyColor = new(0.30f, 0.75f, 1.0f, 0.9f);

    private bool _selfCollide;
    private bool _blocksPlayer;
    private CollisionShape2D _hurtboxShape;

    public CharacterBodyBenchBot()
    {
        CollisionLayer = BenchLayers.Enemies;
        CollisionMask = BenchLayers.Walls;
        // Floating because the arena is top-down; MaxSlides, SafeMargin and PlatformOnLeave stay at the
        // engine defaults (4 / 0.08 px / Additive): no platforms, and a crowd pressing into a corner
        // never needs more slides than the default.
        MotionMode = MotionModeEnum.Floating;

        AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = BenchSpecs.BotRadius },
        });
        AddChild(MakeHurtbox(out _hurtboxShape));
    }

    public Node Node => this;

    public Vector2 Position => GlobalPosition;

    public Vector2 LastDisplacement { get; private set; }

    public void ApplyVelocity(Vector2 velocity)
    {
        Vector2 before = GlobalPosition;
        Velocity = velocity;
        MoveAndSlide();
        LastDisplacement = GlobalPosition - before;
    }

    public void SetCollidesWithEnemies(bool enabled)
    {
        _selfCollide = enabled;
        UpdateMask();
    }

    public void SetBlocksPlayer(bool enabled)
    {
        _blocksPlayer = enabled;
        UpdateMask();
    }

    public void SetHurtboxEnabled(bool enabled)
    {
        _hurtboxShape.SetDeferred(CollisionShape2D.PropertyName.Disabled, !enabled);
    }

    public void PlaceAt(Vector2 globalPosition)
    {
        GlobalPosition = globalPosition;
        Velocity = Vector2.Zero;
        LastDisplacement = Vector2.Zero;
        ResetPhysicsInterpolation();
    }

    public void SetContinuousCollision(bool enabled)
    {
        // A no-op: MoveAndSlide sweeps the shape along the whole motion, so the body cannot tunnel the
        // way a discretely integrated one can.
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, BenchSpecs.BotRadius, BodyColor);
    }

    private void UpdateMask()
    {
        CollisionMask = BenchLayers.Walls
                        | (_selfCollide ? BenchLayers.Enemies : 0)
                        | (_blocksPlayer ? BenchLayers.Player : 0);
    }

    private Area2D MakeHurtbox(out CollisionShape2D shape)
    {
        shape = new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = BenchSpecs.BotRadius },
            Disabled = true,
        };
        var hurtbox = new Area2D
        {
            CollisionLayer = BenchLayers.Hurtboxes,
            CollisionMask = 0,
            Monitoring = false,
            Monitorable = true,
        };
        hurtbox.AddChild(shape);
        return hurtbox;
    }
}
