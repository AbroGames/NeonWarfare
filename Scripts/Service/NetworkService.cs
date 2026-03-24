using Godot;

namespace NeonWarfare.Scripts.Service;

public class NetworkService : KludgeBox.Godot.Services.NetworkService
{

    private SceneTree _rootSceneTree;
    private bool _isDedicatedServer;
    
    public void Init(SceneTree rootSceneTree, bool isDedicatedServer)
    {
        _rootSceneTree = rootSceneTree;
        _isDedicatedServer = isDedicatedServer;
    }

    public override bool IsClient()
    {
        return !_isDedicatedServer;
    }

    public override bool IsServer()
    {
        return _rootSceneTree.GetMultiplayer().IsServer();
    }
}