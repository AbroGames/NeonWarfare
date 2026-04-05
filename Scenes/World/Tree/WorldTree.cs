using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.SceneServiceInjection;
using KludgeBox.Godot.Nodes.MpSync;
using NeonWarfare.Scenes.World.Scenes.SyncedScenes;
using NeonWarfare.Scenes.World.Service;
using NeonWarfare.Scenes.World.Tree.Surfaces;
using NeonWarfare.Scenes.World.Tree.Surfaces.Battle;

namespace NeonWarfare.Scenes.World.Tree;

public partial class WorldTree : Node2D
{

    [Child] public Surface Surface  { get; private set; }
    
    [SceneService] private SyncedPackedScenes _syncedPackedScenes;
    [SceneService] private WorldMultiplayerSpawnerService _multiplayerSpawner;

    public override void _Ready()
    {
        Di.Process(this);
    }
    
    public BattleSurface AddBattleSurface()
    {
        //BattleSurface battleSurface = _syncedPackedScenes.BattleSurface.Instantiate<BattleSurface>();
        //this.AddChildWithUniqueName(battleSurface, "BattleSurface");
        //_battleSurfacesNames.Add(battleSurface.Name);
        //_multiplayerSpawner.AddSpawnerToNode(battleSurface);
        //return battleSurface;

        return null; //TODO Surface changes logic, удалить из tscn ноды дефолтные, создавать в ready
    }
}