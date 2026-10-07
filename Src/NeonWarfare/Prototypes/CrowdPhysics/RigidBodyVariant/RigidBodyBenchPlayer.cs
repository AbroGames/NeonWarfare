using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>The player body of variant B: a RigidBody2D steered by direct velocity writes.</summary>
public partial class RigidBodyBenchPlayer : RigidBodyBenchBody, IBenchPlayerBody
{
    private static readonly Color BodyColor = new(1.0f, 0.85f, 0.2f);

    private bool _blocksEnemies;
    private float _facing;

    public RigidBodyBenchPlayer() : base(BenchSpecs.PlayerRadius, BenchLayers.Player)
    {
    }

    public void SetFacing(float angleRadians)
    {
        _facing = angleRadians;
    }

    public void SetBlocksEnemies(bool enabled)
    {
        _blocksEnemies = enabled;
        UpdateMask();
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

    private void UpdateMask()
    {
        CollisionMask = BenchLayers.Walls | (_blocksEnemies ? BenchLayers.Enemies : 0);
    }
}
