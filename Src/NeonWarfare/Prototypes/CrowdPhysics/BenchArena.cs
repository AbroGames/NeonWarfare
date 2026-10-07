using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The arena: closed borders plus obstacles (boxes, a long wall with a gap, a narrow corridor) that
/// squeeze the crowd. Also owns the spawn logic: a free point is a random point outside every rectangle
/// inflated by the body radius.
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
        Vector2 half = BenchSpecs.ArenaHalfExtents;
        float t = BenchSpecs.WallThickness;

        AddWall(new Rect2(new Vector2(-half.X - t, -half.Y - t), new Vector2(2 * half.X + 2 * t, t)));
        AddWall(new Rect2(new Vector2(-half.X - t, half.Y), new Vector2(2 * half.X + 2 * t, t)));
        AddWall(new Rect2(new Vector2(-half.X - t, -half.Y), new Vector2(t, 2 * half.Y)));
        AddWall(new Rect2(new Vector2(half.X, -half.Y), new Vector2(t, 2 * half.Y)));

        // The long wall with a gap (x in [-700, -500]) that the Wall steering target sits behind.
        AddWall(new Rect2(new Vector2(-1600f, -520f), new Vector2(900f, t)));
        AddWall(new Rect2(new Vector2(-500f, -520f), new Vector2(1100f, t)));

        AddWall(new Rect2(new Vector2(-1275f, 625f), new Vector2(150f, 150f)));
        AddWall(new Rect2(new Vector2(-975f, 685f), new Vector2(150f, 150f)));
        AddWall(new Rect2(new Vector2(1425f, -875f), new Vector2(150f, 150f)));
        AddWall(new Rect2(new Vector2(1625f, 25f), new Vector2(150f, 150f)));
        AddWall(new Rect2(new Vector2(-75f, 225f), new Vector2(150f, 150f)));

        // The narrow corridor: a 120 px passage at x = 1000 that the chase crowd has to squeeze through.
        AddWall(new Rect2(new Vector2(980f, -half.Y), new Vector2(t, 1250f)));
        AddWall(new Rect2(new Vector2(980f, -130f), new Vector2(t, 1630f)));

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

    private void AddWall(Rect2 rect)
    {
        var body = new StaticBody2D
        {
            Position = rect.GetCenter(),
            CollisionLayer = BenchLayers.Walls,
            CollisionMask = 0,
        };
        body.AddChild(new CollisionShape2D
        {
            Shape = new RectangleShape2D { Size = rect.Size },
        });
        AddChild(body);
        _rects.Add(rect);
    }
}
