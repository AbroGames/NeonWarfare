using Godot;
using NeonWarfare.Scenes.KludgeBox;
using NeonWarfare.Scenes.Root.Starters;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Root;

public partial class Root : Node2D
{
    
    [Child] private NodeContainer MainSceneContainer { get; set; }
    [Child] private NodeContainer LoadingScreenContainer { get; set; }
    [Child] private RootPackedScenes PackedScenes { get; set; }

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
