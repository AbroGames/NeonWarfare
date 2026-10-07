using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// The bot body of variant B. With the `C` toggle on, the solver resolves enemy-enemy overlaps — this
/// is the real comparison point against variant A's baseline jitter.
/// </summary>
public partial class RigidBodyBenchBot : RigidBodyBenchBody, IBenchBotBody
{
    private static readonly Color BodyColor = new(0.30f, 0.75f, 1.0f, 0.9f);

    private bool _selfCollide;
    private bool _blocksPlayer;
    private CollisionShape2D _hurtboxShape;

    public RigidBodyBenchBot() : base(BenchSpecs.BotRadius, BenchLayers.Enemies)
    {
        AddChild(MakeHurtbox(out _hurtboxShape));
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
