using System.Linq;
using Godot;
using GodotBox;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Root.Starters;
using NeonWarfare.Scripts.Content.CmdArgs;

namespace NeonWarfare.Scenes.Root;

public partial class Root : Node2D
{
    [Child] public NodeContainer MainSceneContainer { get; set; }
    [Child] public NodeContainer LoadingScreenContainer { get; set; }
    [Child] public RootPackedScenes PackedScenes { get; set; }

    private BaseRootStarter _rootStarter;
    
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
        _rootStarter = ChooseStarter(rootData);
        _rootStarter.Init();
    }

    private void Start()
    {
        _rootStarter.Start();
    }

    private static BaseRootStarter ChooseStarter(RootData rootData)
    {
        return OS.GetCmdlineArgs().Contains(DedicatedServerArgs.DedicatedServerFlag) ? 
            new DedicatedServerRootStarter(rootData) : 
            new ClientRootStarter(rootData);
    }
}
