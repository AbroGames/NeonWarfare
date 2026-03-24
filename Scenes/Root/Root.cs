using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Root.Starters;
using Serilog;

namespace NeonWarfare.Scenes.Root;

public partial class Root : Node2D
{
    [Child] public NodeContainer MainSceneContainer { get; set; }
    [Child] public NodeContainer LoadingScreenContainer { get; set; }
    [Child] public RootPackedScenes PackedScenes { get; set; }

    private RootStarterManager _rootStarterManager;
    
    public override void _Ready()
    {
        Di.Process(this);
        
        Callable.From(() => {
            Init();
            Start();
        }).CallDeferred();
    }

    private void Init()
    {
        RootData rootData = new RootData(MainSceneContainer, LoadingScreenContainer, PackedScenes, GetTree());
        _rootStarterManager = new RootStarterManager(rootData);
        _rootStarterManager.Init();
    }

    private void Start()
    {
        _rootStarterManager.Start();
    }
}
