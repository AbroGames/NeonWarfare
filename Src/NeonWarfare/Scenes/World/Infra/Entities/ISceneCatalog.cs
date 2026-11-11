using Godot;

namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// The scenes World may spawn, as Infra sees them: the catalog itself lists the scenes of features, which Infra must
/// not know.
/// </summary>
public interface ISceneCatalog
{
    /// <exception cref="System.ArgumentException">The scene is not in the catalog.</exception>
    int GetSceneId(PackedScene scene);
}
