using Godot;

namespace NeonWarfare.Scenes.World.Entities;

/// <summary>
/// The World node or some child as a spawn root. The services get this instead of <see cref="World"/>
/// </summary>
public class WorldRoot(Node spawnRoot)
{
    public void AddChild(Node child) => spawnRoot.AddChild(child);
}
