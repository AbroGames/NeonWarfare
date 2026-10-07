using System.Linq;
using Godot;
using NeonWarfare.Scripts.Content.CmdArgs;

namespace NeonWarfare.Scenes.Root.Starters;

public class RootStarterManager
{

    private readonly RootData _rootData;
    private readonly BaseRootStarter _rootStarter;

    public RootStarterManager(RootData rootData)
    {
        _rootData = rootData;
        _rootStarter = ChooseStarter();
    }
    
    public void Init()
    {
        _rootStarter.Init(_rootData);
    }
    
    public void Start()
    {
        _rootStarter.Start(_rootData);
    }

    private BaseRootStarter ChooseStarter()
    {
        var cmdline = OS.GetCmdlineArgs();
        if (cmdline.Contains(DedicatedServerArgs.DedicatedServerFlag))
        {
            return new DedicatedServerRootStarter();
        }
        if (cmdline.Contains(PhysicsBenchmarkArgs.PhysicsBenchmarkFlag))
        {
            return new PhysicsBenchmarkRootStarter();
        }
        return new ClientRootStarter();
    }
}