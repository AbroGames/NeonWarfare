using NeonWarfare.Scenes.Root;

namespace NeonWarfare.Scripts.Service;

public class NetworkService : KludgeBox.Godot.Services.NetworkService
{

    private Root _root;
    private bool _isDedicatedServer;
    
    public void Init(Root root, bool isDedicatedServer)
    {
        _root = root;
        _isDedicatedServer = isDedicatedServer;
    }

    public override bool IsClient()
    {
        return !_isDedicatedServer;
    }

    public override bool IsServer()
    {
        return _root.GetTree().GetMultiplayer().IsServer();
    }
}