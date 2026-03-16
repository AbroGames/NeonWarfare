using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service;
using NeonWarfare.Scenes.World.Tree.Surface.Battle;
using NeonWarfare.Scenes.World.Tree.Surface.Map;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using KludgeBox.Godot.Nodes.MpSync;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{

    [Child] public MapSurface MapSurface  { get; private set; }
    
    public List<BattleSurface> BattleSurfaces => _battleSurfacesNames.Select(name => GetNodeOrNull<BattleSurface>(name)).ToList();
    [Export] [Sync] private Array<string> _battleSurfacesNames = new();
    
    [SceneService] private SyncedPackedScenes _syncedPackedScenes;
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawner;

    public override void _Ready()
    {
        Di.Process(this);
    }
    
    public BattleSurface AddBattleSurface()
    {
        BattleSurface battleSurface = _syncedPackedScenes.BattleSurface.Instantiate<BattleSurface>();
        this.AddChildWithUniqueName(battleSurface, "BattleSurface");
        _battleSurfacesNames.Add(battleSurface.Name);
        _multiplayerSpawner.AddSpawnerToNode(battleSurface);
        return battleSurface;
    }
}