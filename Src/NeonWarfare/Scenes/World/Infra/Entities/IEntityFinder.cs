using System;
using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// The read side of <see cref="EntityRegistry"/>, open to every layer. Registering stays with the layers that
/// spawn, or a NetId could be taken past the generator.
/// </summary>
public interface IEntityFinder
{
    /// <summary>
    /// Raised once the node is in its place and in the registry. Not necessarily after its <c>_Ready</c>: an entity
    /// spawned while the World is outside the tree gets it only when the World enters.
    /// </summary>
    event Action<NetId, Node> SpawnedEvent;

    /// <summary>
    /// Raised once the node is out of the registry, inside its <c>TreeExiting</c>: the node is still in the tree and
    /// may be in the middle of being freed, so a subscriber must not change the tree. Freeing a whole World raises it
    /// for every entity.
    /// </summary>
    event Action<NetId, Node> DespawnedEvent;

    bool TryGetNode(NetId id, out Node node);

    Node GetNode(NetId id);

    bool TryGetNetId(Node node, out NetId id);

    /// <summary>The <see cref="EntityCatalog"/> kind the entity was created from.</summary>
    /// <exception cref="KeyNotFoundException">The NetId is not registered.</exception>
    int GetKindId(NetId id);

    /// <summary>
    /// Every registered node that is a <typeparamref name="T"/>, base classes and interfaces included, ordered by
    /// NetId. The same instance is returned while the set is unchanged; a returned list never changes.
    /// </summary>
    IReadOnlyList<T> GetAll<T>() where T : class;

    /// <summary>The only registered <typeparamref name="T"/>; none or several throw.</summary>
    T GetSingle<T>() where T : class;

    /// <summary>Whether any registered node is a <typeparamref name="T"/>.</summary>
    bool Exists<T>() where T : class;
}
