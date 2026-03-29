using NeonWarfare.Scenes.Game.Network;

namespace NeonWarfare.Scripts.Service;

public class NetworkService : KludgeBox.Godot.Services.NetworkService
{

    private bool _isDedicatedServer;
    private Network _network;
    
    public void Init(bool isDedicatedServer)
    {
        _isDedicatedServer = isDedicatedServer;
    }

    public void SetGameNetwork(Network network)
    {
        _network = network;
    }
    
    public void RemoveGameNetwork() => SetGameNetwork(null);

    public override bool IsClient()
    {
        return !_isDedicatedServer;
    }

    /// <summary>
    /// Default state for MainMenu, SingleplayerGame is Server=true.
    /// Because in this situation game don't have outer controller.<br/>
    /// Server=false only when game connected to other server.
    /// </summary>
    public override bool IsServer()
    {
        bool isConnected = Services.MainScene.MainSceneIsGame() &&
                           _network != null &&
                           _network.StateMachine != null &&
                           _network.StateMachine.IsActiveGameState &&
                           _network.StateMachine.IsClient;
        
        return !isConnected;
    }
}