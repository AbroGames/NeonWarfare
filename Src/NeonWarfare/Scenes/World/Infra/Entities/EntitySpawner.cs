using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.World.Infra.Composition;

namespace NeonWarfare.Scenes.World.Infra.Entities;

/// <summary>
/// Creates an entity on the server: a kind of the <see cref="EntityCatalog"/> (a scene, or a node type with no
/// scene of its own), a fresh NetId, a place in the tree and in the registry. Despawns it too.
/// </summary>
[Simulation]
public class EntitySpawner(
    NetIdGenerator netIdGenerator, EntityRegistry registry, WorldRoot root, EntityCatalog catalog)
{
    private const string WrongTypeError = "The root of {0} is {1}, not {2}.";
    private const string NotRegisteredError = "{0} is not a registered entity.";
    private const string OutOfTreeError = "{0} is outside the tree: the registry would not see it leave.";

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
            throw new ArgumentException(WrongTypeError.FormatWith(catalog.Descriptors[kindId], actual, typeof(T).Name));
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

        // Last, so a SpawnedEvent subscriber finds the node already in its place
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
            throw new ArgumentException(NotRegisteredError.FormatWith(node.Name), nameof(node));
        }
        if (!node.IsInsideTree())
        {
            throw new InvalidOperationException(OutOfTreeError.FormatWith(node.Name));
        }

        node.GetParent().RemoveChild(node);
        node.QueueFree();
    }
}
