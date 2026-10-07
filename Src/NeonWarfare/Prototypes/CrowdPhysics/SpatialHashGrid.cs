using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Uniform grid (spatial hash) for the soft separation queries, rebuilt every physics tick. Cells hold
/// bot indices; a query walks the 3x3 cells around a point and filters by distance. The dictionary and
/// its lists are kept and cleared between ticks, so steady state allocates nothing.
/// </summary>
public class SpatialHashGrid
{
    private readonly float _cellSize;
    private readonly Dictionary<int, List<int>> _cells = new();
    private readonly List<Vector2> _positions = [];

    public SpatialHashGrid(float cellSize)
    {
        _cellSize = cellSize;
    }

    public void Rebuild(IReadOnlyList<BotUnit> bots)
    {
        foreach (List<int> cell in _cells.Values)
        {
            cell.Clear();
        }
        _positions.Clear();

        for (int i = 0; i < bots.Count; i++)
        {
            Vector2 position = bots[i].Body.Position;
            _positions.Add(position);
            int key = KeyOf(position);
            if (!_cells.TryGetValue(key, out List<int> cell))
            {
                cell = [];
                _cells[key] = cell;
            }
            cell.Add(i);
        }
    }

    /// <summary>Collects the indices of bots within `radius` of `position` into `result` (cleared first).</summary>
    public void CollectNeighbours(Vector2 position, float radius, List<int> result)
    {
        result.Clear();
        float radiusSquared = radius * radius;

        Vector2I center = CellOf(position);
        for (int y = center.Y - 1; y <= center.Y + 1; y++)
        {
            for (int x = center.X - 1; x <= center.X + 1; x++)
            {
                if (!_cells.TryGetValue(KeyOf(x, y), out List<int> cell))
                {
                    continue;
                }

                foreach (int index in cell)
                {
                    if (position.DistanceSquaredTo(_positions[index]) <= radiusSquared)
                    {
                        result.Add(index);
                    }
                }
            }
        }
    }

    private Vector2I CellOf(Vector2 position) =>
        new(Mathf.FloorToInt(position.X / _cellSize), Mathf.FloorToInt(position.Y / _cellSize));

    private int KeyOf(Vector2 position)
    {
        Vector2I cell = CellOf(position);
        return KeyOf(cell.X, cell.Y);
    }

    private static int KeyOf(int x, int y) => (int)((uint)x * 73856093u ^ (uint)y * 19349663u);
}
