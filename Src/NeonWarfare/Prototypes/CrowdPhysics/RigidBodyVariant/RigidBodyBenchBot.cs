using System;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// The bot body of variant B. With the `C` toggle on, the solver resolves enemy-enemy overlaps — this
/// is the real comparison point against variant A's baseline jitter.
/// </summary>
public partial class RigidBodyBenchBot : RigidBodyBenchBody, IBenchBotBody
{
    private const float ArrowLength = 12f;
    private const float ArrowHeadLength = 6f;
    private const float ArrowHeadHalfWidth = 3.5f;
    private const float ArrowWidth = 2f;

    /// <summary>Displacement below this (px per tick) counts as standing: an arrow would only quiver
    /// against the obstacle the bot is pressed into.</summary>
    private const float MinArrowDisplacement = 0.5f;

    private readonly float _radius;
    private readonly float _arrowScale;
    private readonly Color _bodyColor = BenchSpecs.BotColors[Random.Shared.Next(BenchSpecs.BotColors.Length)];

    private bool _selfCollide;
    private bool _blocksPlayer;
    private CollisionShape2D _hurtboxShape;

    public RigidBodyBenchBot(bool big) : base(
        big ? BenchSpecs.BigBotRadius : BenchSpecs.BotRadius, BenchLayers.Enemies,
        big ? BenchSpecs.BigBotMass : 1f)
    {
        _radius = big ? BenchSpecs.BigBotRadius : BenchSpecs.BotRadius;
        _arrowScale = _radius / BenchSpecs.BotRadius;
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

    // The canvas item is cached between redraws, so without this the arrow would freeze at its first
    // direction; the bot only changes what it draws, its motion is pure physics.
    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, _radius, _bodyColor);

        // The body never rotates (LockRotation), so local axes are the global ones and the global
        // displacement can be drawn as-is.
        Vector2 displacement = LastDisplacement;
        if (displacement.LengthSquared() < MinArrowDisplacement * MinArrowDisplacement) return;

        Vector2 direction = displacement.Normalized();
        Vector2 tip = direction * (_radius + ArrowLength * _arrowScale);
        DrawLine(direction * _radius, tip, _bodyColor, ArrowWidth * _arrowScale);

        Vector2 back = tip - direction * (ArrowHeadLength * _arrowScale);
        DrawColoredPolygon(new[] { back + direction.Orthogonal() * (ArrowHeadHalfWidth * _arrowScale), tip,
            back - direction.Orthogonal() * (ArrowHeadHalfWidth * _arrowScale) }, _bodyColor);
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
            Shape = new CircleShape2D { Radius = _radius },
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
