using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service;
using NeonWarfare.Scenes.World.Tree.Surfaces;
using NeonWarfare.Scenes.World.Tree.Surfaces.Battle;
using NeonWarfare.Scenes.World.Tree.Surfaces.Safe;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{

    public Surface Surface  { get; private set; }
    
    [SceneService] private SyncedPackedScenes _syncedPackedScenes;
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawner;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public SafeSurface SetSafeSurface()
    {
        Surface?.QueueFree();
        
        SafeSurface safeSurface = _syncedPackedScenes.SafeSurface.Instantiate<SafeSurface>();
        this.AddChildWithUniqueName(safeSurface, "SafeSurface");
        Surface = safeSurface;
        _multiplayerSpawner.AddSpawnerToNode(safeSurface);
        return safeSurface;
    }
    
    public BattleSurface SetBattleSurface()
    {
        Surface?.QueueFree();
        
        BattleSurface battleSurface = _syncedPackedScenes.BattleSurface.Instantiate<BattleSurface>();
        this.AddChildWithUniqueName(battleSurface, "BattleSurface");
        Surface = battleSurface;
        _multiplayerSpawner.AddSpawnerToNode(battleSurface);
        return battleSurface;
    }
}