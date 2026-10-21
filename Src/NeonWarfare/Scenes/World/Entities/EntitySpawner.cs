using System;
using Godot;
using Humanizer;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.ServerNetwork;

namespace NeonWarfare.Scenes.World.Entities;

/// <summary>
/// Creates an entity on the server: a scene of <see cref="WorldPackedScenes"/>, a fresh NetId, a place in the tree
/// and in the registry.
/// </summary>
[Simulation]
public class EntitySpawner(
    NetIdGenerator netIdGenerator, EntityRegistry registry, WorldRoot root, WorldPackedScenes scenes)
{
    private const string WrongTypeError = "The root of {0} is {1}, not {2}.";

    public T SpawnOnRoot<T>(PackedScene scene, Action<T> initPreReady = null) where T : Node =>
        Spawn(scene, NetId.None, initPreReady);

    /// <param name="parent"><see cref="NetId.None"/> for the World root.</param>
    /// <param name="initPreReady">
    /// Sets the entity up before it enters the tree, so its <c>_Ready</c> and the registry's subscribers see it
    /// configured.
    /// </param>
    public T Spawn<T>(PackedScene scene, NetId parent, Action<T> initPreReady = null) where T : Node
    {
        // Checks that the scene is in the catalog
        scenes.GetSceneId(scene);
        // Checks that the parent scene is in the registry
        Node parentNode = parent == NetId.None ? null : registry.GetNode(parent);
        
        // Only an instance knows its C# class: the scene itself knows just the engine class of its root
        Node instance = scene.Instantiate();
        if (instance is not T node)
        {
            string actual = instance.GetType().Name;
            instance.Free();
            throw new ArgumentException(WrongTypeError.FormatWith(scene.ResourcePath, actual, typeof(T).Name),
                nameof(scene));
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
        registry.Register(id, node);
        return node;
    }
}
