using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare.NetOld.Client;

[GameService]
public class GodotClientService
{

    [EventListener]
    public void OnPeerConnectedClientEvent(PeerConnectedClientEvent peerConnectedClientEvent)
    {
        Log.Debug($"PeerConnectedClientEvent: {peerConnectedClientEvent.Id}");
    }
    
    [EventListener]
    public void OnPeerDisconnectedClientEvent(PeerDisconnectedClientEvent peerDisconnectedClientEvent)
    {
        Log.Debug($"PeerDisconnectedClientEvent: {peerDisconnectedClientEvent.Id}");
    }
    
    [EventListener]
    public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
    {
        Log.Debug("ConnectedToServerEvent");
        Root.Instance.GetWindow().MoveToForeground();
    }
    
    [EventListener]
    public void OnConnectionToServerFailedEvent(ConnectionToServerFailedEvent connectionToServerFailedEvent)
    {
        Log.Debug("ConnectionToServerFailedEvent");
    }
    
    [EventListener]
    public void OnServerDisconnectedEvent(ServerDisconnectedEvent serverDisconnectedEvent)
    {
        Log.Debug("ServerDisconnectedEvent");
    }
}