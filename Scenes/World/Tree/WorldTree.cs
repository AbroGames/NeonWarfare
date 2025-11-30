using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.ParentInjection;
using KludgeBox.Godot.Nodes.MpSync;
using BattleSurface = NeonWarfare.Scenes.World.Tree.Surface.Battle.BattleSurface;
using MapSurface = NeonWarfare.Scenes.World.Tree.Surface.Map.MapSurface;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{

    [Child] public MapSurface MapSurface  { get; private set; }
    
    public List<BattleSurface> BattleSurfaces => _battleSurfacesNames.Select(name => GetNodeOrNull<BattleSurface>(name)).ToList();
    [Export] [Sync] private Array<string> _battleSurfacesNames = new();
    
    [Parent] private World _world;

    public override void _Ready()
    {
        Di.Process(this);
    }
    
    public BattleSurface AddBattleSurface()
    {
        BattleSurface battleSurface = _world.WorldPackedScenes.BattleSurface.Instantiate<BattleSurface>();
        this.AddChildWithUniqueName(battleSurface, "BattleSurface");
        _battleSurfacesNames.Add(battleSurface.Name);
        _world.MultiplayerSpawnerService.AddSpawnerToNode(battleSurface);
        return battleSurface;
    }
}