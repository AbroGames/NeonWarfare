using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>A runtime wall segment created with `F` and removed with `G` (last created first).</summary>
public partial class BenchWall : StaticBody2D
{
    public const float HalfLength = 150f;
    public const float HalfThickness = 15f;

    private static readonly Color FillColor = new(0.85f, 0.55f, 0.15f, 0.85f);
    private static readonly Color BorderColor = new(1.00f, 0.75f, 0.35f);

    public BenchWall()
    {
        CollisionLayer = BenchLayers.Walls;
        CollisionMask = 0;
        AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D
            {
                Size = new Vector2(2 * HalfLength, 2 * HalfThickness),
            },
        });
    }

    public override void _Draw()
    {
        var rect = new Rect2(-HalfLength, -HalfThickness, 2 * HalfLength, 2 * HalfThickness);
        DrawRect(rect, FillColor);
        DrawRect(rect, BorderColor, false, 2f);
    }
}
