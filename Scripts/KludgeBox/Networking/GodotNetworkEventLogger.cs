using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Networking;

[GameService]
public class GodotClientService
{

    [EventListener]
    public void OnPeerConnectedClientEvent(PeerConnectedEvent peerConnectedClientEvent)
    {
        Log.Debug($"Network event: PeerConnected(id={peerConnectedClientEvent.Id})");
    }
    
    [EventListener]
    public void OnPeerDisconnectedClientEvent(PeerDisconnectedEvent peerDisconnectedClientEvent)
    {
        Log.Debug($"Network event: PeerDisconnected(id={peerDisconnectedClientEvent.Id})");
    }
    
    [EventListener]
    public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
    {
        Log.Debug("Network event: ConnectedToServer");
    }
    
    [EventListener]
    public void OnConnectionToServerFailedEvent(ConnectionToServerFailedEvent connectionToServerFailedEvent)
    {
        Log.Debug("Network event: ConnectionToServerFailed");
    }
    
    [EventListener]
    public void OnServerDisconnectedEvent(ServerDisconnectedEvent serverDisconnectedEvent)
    {
        Log.Debug("Network event: ServerDisconnected");
    }
}