using Godot;
using NeonWarfare.Scenes.World.MpSpawn;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.World.Services;

public partial class WorldMultiplayerSpawnerService : Node
{
    
    [Export] [NotNull] public PackedScene WorldMultiplayerSpawnerPackedScene { get; private set; }
    
    /// <summary>
    /// You can use this method, if <c>observableNode</c> <b>already in scene tree</b>.<br/>
    /// If <c>observableNode</c> not in scene tree yet, you must use <c>AddSpawnerToNode(Node observableNode, Node parentNode)</c>.
    /// </summary>
    /// <param name="observableNode">
    /// <see cref="MultiplayerSpawner"/> will observe this node and sync children of <c>observableNode</c> by network.<br/>
    /// <see cref="MultiplayerSpawner"/> will be added as sibling of this node.
    /// </param>
    /// <returns>Created spawner</returns>
    public WorldMultiplayerSpawner AddSpawnerToNode(Node observableNode)
    {
        return AddSpawnerToNode(observableNode, observableNode.GetParent());
    }
    
    /// <summary>
    /// You must use this method, if <c>observableNode</c> <b>not in scene tree yet</b>.<br/>
    /// If <c>observableNode</c> in scene tree, you can use <c>AddSpawnerToNode(Node observableNode)</c>.
    /// </summary>
    /// <param name="observableNode"><see cref="MultiplayerSpawner"/> will observe this node and sync children of <c>observableNode</c> by network</param>
    /// <param name="parentNode"><see cref="MultiplayerSpawner"/> will be added as child of this node</param>
    /// <returns>Created spawner</returns>
    public WorldMultiplayerSpawner AddSpawnerToNode(Node observableNode, Node parentNode)
    {
        WorldMultiplayerSpawner worldMultiplayerSpawner = WorldMultiplayerSpawnerPackedScene.Instantiate<WorldMultiplayerSpawner>(); 
        worldMultiplayerSpawner.InitPreReady(observableNode);
        parentNode.AddChildWithName(worldMultiplayerSpawner, observableNode.GetName() + "-MultiplayerSpawner");
        observableNode.TreeExiting += worldMultiplayerSpawner.QueueFree;
        return worldMultiplayerSpawner;
    }
}