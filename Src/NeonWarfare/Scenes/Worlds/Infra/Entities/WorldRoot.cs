using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// The node entities are spawned under. The services get it instead of <see cref="World"/>.
/// </summary>
public class WorldRoot(Node spawnRoot)
{
    public void AddChild(Node child) => spawnRoot.AddChild(child);
}
