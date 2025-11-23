using Godot;
using NeonWarfare.Scenes.Root.Starters;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;
using NodeContainer = NeonWarfare.Scenes.KludgeBox.NodeContainer;

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
        _rootStarterManager = new RootStarterManager(this);
        _rootStarterManager.Init();
    }

    private void Start()
    {
        _rootStarterManager.Start();
    }
}
