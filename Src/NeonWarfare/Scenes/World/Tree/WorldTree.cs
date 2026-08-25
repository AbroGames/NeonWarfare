using Godot;
using KludgeBox.DI.Requests.SceneServiceInjection;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.WorldServices;
using NeonWarfare.Scenes.World.Tree.Surfaces;
using NeonWarfare.Scenes.World.Tree.Surfaces.Battle;
using NeonWarfare.Scenes.World.Tree.Surfaces.Safe;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{

    public Surface Surface { get; private set; }
    
    [SceneService] private SyncedPackedScenes _syncedPackedScenes;
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawner;

    public override void _Ready()
    {
        Di.Process(this);
        
        // Init default surface as placeholder for avoid NullException in other services.
        // Surface will be init again as new node in server init section 
        Surface = _syncedPackedScenes.SafeSurface.Instantiate<SafeSurface>();
    }

    public SafeSurface SetSafeSurface()
    {
        Surface?.QueueFree();
        
        SafeSurface safeSurface = _syncedPackedScenes.SafeSurface.Instantiate<SafeSurface>();
        this.AddChildWithUniqueName(safeSurface, "SafeSurface");
        _multiplayerSpawner.AddSpawnerToNode(safeSurface);
        Surface = safeSurface;
        safeSurface.InitOnServer();
        
        return safeSurface;
    }
    
    public BattleSurface SetBattleSurface()
    {
        Surface?.QueueFree();
        
        BattleSurface battleSurface = _syncedPackedScenes.BattleSurface.Instantiate<BattleSurface>();
        this.AddChildWithUniqueName(battleSurface, "BattleSurface");
        _multiplayerSpawner.AddSpawnerToNode(battleSurface);
        Surface = battleSurface;
        battleSurface.InitOnServer();
        
        return battleSurface;
    }

    public SafeSurface GetSafeSurfaceOrNull()
    {
        return Surface as SafeSurface;
    }
    
    public BattleSurface GetBattleSurfaceOrNull()
    {
        return Surface as BattleSurface;
    }

    public override void _Notification(int id)
    {
        if (id == NotificationPredelete) FreePlaceholderSurface();
    }

    /// <summary>
    /// The placeholder from <see cref="_Ready"/> never enters the tree, so freeing the tree does not reach it.
    /// The server replaces and frees it in <see cref="SetSafeSurface"/>, but a pure client never calls that —
    /// its surfaces come from the spawner — and would leak the placeholder with every session.
    /// </summary>
    private void FreePlaceholderSurface()
    {
        if (Surface is not null && Surface.GetParent() is null) Surface.Free();
    }
}