using System;
using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Entities;

/// <summary>
/// Creates an entity on the server: a kind of the <see cref="EntityCatalog"/> (a scene, or a node type with no
/// scene of its own), a fresh NetId, a place in the tree and in the registry. Despawns it too.
/// </summary>
[Simulation]
public class EntitySpawner(
    NetIdGenerator netIdGenerator, EntityRegistry registry, WorldRoot root, EntityCatalog catalog)
{
    public T SpawnOnRoot<T>(PackedScene scene, Action<T> initPreReady = null) where T : Node =>
        Spawn(scene, NetId.None, initPreReady);

    /// <param name="parent"><see cref="NetId.None"/> for the World root.</param>
    /// <param name="initPreReady">
    /// Sets the entity up before it enters the tree, so its <c>_Ready</c> and the registry's subscribers see it
    /// configured.
    /// </param>
    public T Spawn<T>(PackedScene scene, NetId parent, Action<T> initPreReady = null) where T : Node =>
        Spawn(catalog.GetKindId(scene), parent, initPreReady);

    /// <summary>An entity without a scene: a bare <typeparamref name="T"/>, created by its constructor.</summary>
    public T SpawnOnRoot<T>(Action<T> initPreReady = null) where T : Node =>
        Spawn(NetId.None, initPreReady);

    /// <inheritdoc cref="SpawnOnRoot{T}(Action{T})"/>
    /// <param name="parent"><see cref="NetId.None"/> for the World root.</param>
    /// <param name="initPreReady">See <see cref="Spawn{T}(PackedScene, NetId, Action{T})"/>.</param>
    public T Spawn<T>(NetId parent, Action<T> initPreReady = null) where T : Node =>
        Spawn(catalog.GetKindId(typeof(T)), parent, initPreReady);

    private T Spawn<T>(int kindId, NetId parent, Action<T> initPreReady) where T : Node
    {
        // Checks that the parent is in the registry
        Node parentNode = parent == NetId.None ? null : registry.GetNode(parent);

        // Only an instance knows its C# class: a scene itself knows just the engine class of its root
        Node instance = catalog.Create(kindId);
        if (instance is not T node)
        {
            string actual = instance.GetType().Name;
            instance.Free();
            throw new ArgumentException(
                $"The root of {catalog.Descriptors[kindId]} is {actual}, not {typeof(T).Name}.");
        }

        // Call InitPreReady callback
        try
        {
            initPreReady?.Invoke(node);
        }
        catch
        {
            node.Free();
            throw;
        }

        // Gen id and to the parent
        NetId id = netIdGenerator.Next();
        if (parentNode == null)
        {
            root.AddChild(node);
        }
        else
        {
            parentNode.AddChild(node);
        }

        // Last, so a Spawned subscriber finds the node already in its place
        registry.Register(id, node, kindId);
        return node;
    }

    /// <summary>
    /// Removes the entity with its whole subtree, every registered descendant included. The node is freed at the end
    /// of the frame.
    /// </summary>
    public void Despawn(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!registry.TryGetNetId(node, out _))
        {
            throw new ArgumentException($"{node.Name} is not a registered entity.", nameof(node));
        }
        if (!node.IsInsideTree())
        {
            throw new InvalidOperationException(
                $"{node.Name} is outside the tree: the registry would not see it leave.");
        }

        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}
