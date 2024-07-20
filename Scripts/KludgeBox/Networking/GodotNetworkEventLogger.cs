using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Networking;

public static class GodotClientService
{

    [EventListener]
    public static void OnPeerConnectedClientEvent(PeerConnectedEvent peerConnectedClientEvent)
    {
        Log.Debug($"Network event: PeerConnected(id={peerConnectedClientEvent.Id})");
    }
    
    [EventListener]
    public static void OnPeerDisconnectedClientEvent(PeerDisconnectedEvent peerDisconnectedClientEvent)
    {
        Log.Debug($"Network event: PeerDisconnected(id={peerDisconnectedClientEvent.Id})");
    }
    
    [EventListener]
    public static void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
    {
        Log.Debug("Network event: ConnectedToServer");
    }
    
    [EventListener]
    public static void OnConnectionToServerFailedEvent(ConnectionToServerFailedEvent connectionToServerFailedEvent)
    {
        Log.Debug("Network event: ConnectionToServerFailed");
    }
    
    [EventListener]
    public static void OnServerDisconnectedEvent(ServerDisconnectedEvent serverDisconnectedEvent)
    {
        Log.Debug("Network event: ServerDisconnected");
    }
}