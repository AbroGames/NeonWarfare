using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The arena: an open field with no walls. Also owns the spawn logic: a free point is a random point
/// outside every rectangle inflated by the body radius.
/// </summary>
public partial class BenchArena : Node2D
{
    private static readonly Color FillColor = new(0.16f, 0.18f, 0.22f);
    private static readonly Color BorderColor = new(0.45f, 0.50f, 0.60f);

    /// <summary>The point the `Wall` steering mode drives the bots to: behind the long wall, reachable
    /// only through its gap, so the crowd piles up against the wall face.</summary>
    public static readonly Vector2 WallCoverPoint = new(-600f, -1100f);

    private readonly List<Rect2> _rects = [];

    public IReadOnlyList<Rect2> Rects => _rects;

    public override void _Ready()
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach (Rect2 rect in _rects)
        {
            DrawRect(rect, FillColor);
            DrawRect(rect, BorderColor, false, 2f);
        }
    }

    public bool IsPointFree(Vector2 point, float bodyRadius)
    {
        foreach (Rect2 rect in _rects)
        {
            if (rect.Grow(bodyRadius + 4f).HasPoint(point))
            {
                return false;
            }
        }
        return true;
    }
}
