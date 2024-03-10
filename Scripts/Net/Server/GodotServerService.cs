using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class GodotServerService
{

    [EventListener]
    public void OnPeerConnectedServerEvent(PeerConnectedServerEvent peerConnectedServerEvent)
    {
        Log.Debug($"PeerConnectedServerEvent: {peerConnectedServerEvent.Id}");
        
        PlayerServerInfo playerServerInfo = new PlayerServerInfo(peerConnectedServerEvent.Id);
        Root.Instance.Server.PlayerServerInfo.Add(playerServerInfo.Id, playerServerInfo);
        
        Network.SendPacketToPeer(peerConnectedServerEvent.Id,
            new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Safe));

        Node currentMainScene = Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<Node2D>();
        if (currentMainScene is SafeWorldMainScene)
        {
            Network.SendPacketToPeer(peerConnectedServerEvent.Id, 
                new ServerSpawnPlayerPacket(Rand.Range(-200, 200), Rand.Range(-200, 200), Rand.Range(0, 360)));
        } 
        else if (currentMainScene is BattleWorldMainScene)
        {
            Network.SendPacketToPeer(peerConnectedServerEvent.Id, new ServerWaitBattleEndPacket());
        }
        else
        {
            Log.Error($"Unknown MainScene in MainSceneContainer: {currentMainScene}");
        }
    }
    
    [EventListener]
    public void OnPeerDisconnectedServerEvent(PeerDisconnectedServerEvent peerDisconnectedServerEvent)
    {
        Log.Debug($"PeerDisconnectedServerEvent: {peerDisconnectedServerEvent.Id}");

        Root.Instance.Server.PlayerServerInfo[peerDisconnectedServerEvent.Id].Player.QueueFree();
        Root.Instance.Server.PlayerServerInfo.Remove(peerDisconnectedServerEvent.Id);
    }
    
}