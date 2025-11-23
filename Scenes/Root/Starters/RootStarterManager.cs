using System.Linq;
using Godot;
using NeonWarfare.Scripts.Service.CmdArgs;

namespace NeonWarfare.Scenes.Root.Starters;

public class RootStarterManager
{

    private readonly Root _root;
    private readonly BaseRootStarter _rootStarter;

    public RootStarterManager(Root root)
    {
        _root = root;
        _rootStarter = ChooseStarter();
    }
    
    public void Init()
    {
        _rootStarter.Init(_root);
    }
    
    public void Start()
    {
        _rootStarter.Start(_root);
    }

    private BaseRootStarter ChooseStarter()
    {
        return OS.GetCmdlineArgs().Contains(DedicatedServerArgs.DedicatedServerFlag) ? new DedicatedServerRootStarter() : new ClientRootStarter();
    }
}