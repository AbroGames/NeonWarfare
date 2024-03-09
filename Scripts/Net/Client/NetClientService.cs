using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class NetClientService
{
    
    [EventListener]
    public void OnCreateServerRequest(CreateServerRequest createServerRequest)
    {
        Root.Instance.ServerPid = OS.CreateInstance([
            ServerParams.ServerFlag,
            ServerParams.HeadlessFlag,
            ServerParams.PortParam, createServerRequest.Port.ToString(),
            ServerParams.AdminParam, createServerRequest.AdminNickname,
            ServerParams.ParentPidParam, OS.GetProcessId().ToString()
        ]);
    }

    [EventListener]
    public void OnConnectToServerRequest(ConnectToServerRequest connectToServerRequest)
    {
        Network.ConnectToRemoteServer(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port);
    }

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