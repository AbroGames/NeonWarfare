using System;
using System.Collections.Generic;
using Godot;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// The read side of <see cref="EntityRegistry"/>, open to every layer.
/// Only the spawning layers may register, so no NetId is taken past the generator.
/// </summary>
public interface IEntityFinder
{
    /// <summary>
    /// Raised once the node is in its place and in the registry.
    /// Its <c>_Ready</c> may not have run yet, because an entity spawned while the World is outside the tree gets it
    /// only when the World enters the tree.
    /// </summary>
    event Action<NetId, Node> Spawned;

    /// <summary>
    /// Raised once the node is out of the registry, inside its <c>TreeExiting</c>.
    /// The node is still in the tree and may be in the middle of being freed, so a subscriber must not change the tree.
    /// Freeing a whole World raises it for every entity.
    /// </summary>
    event Action<NetId, Node> Despawned;

    bool TryGetNode(NetId id, out Node node);

    Node GetNode(NetId id);

    bool TryGetNetId(Node node, out NetId id);

    /// <summary>The <see cref="EntityCatalog"/> kind the entity was created from.</summary>
    /// <exception cref="KeyNotFoundException">The NetId is not registered.</exception>
    int GetKindId(NetId id);

    /// <summary>
    /// Every registered node that is a <typeparamref name="T"/>, through a base class or an interface too, ordered
    /// by NetId.
    /// A returned list never changes. The same instance is returned while the set stays the same.
    /// </summary>
    IReadOnlyList<T> GetAll<T>() where T : class;

    /// <summary>The only registered <typeparamref name="T"/>. Throws when there are none or several.</summary>
    T GetSingle<T>() where T : class;

    /// <summary>Whether any registered node is a <typeparamref name="T"/>.</summary>
    bool Exists<T>() where T : class;
}
