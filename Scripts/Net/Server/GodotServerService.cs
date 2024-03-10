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

        Node currentWorld = Root.Instance.CurrentWorld;
        if (currentWorld is SafeWorld)
        {
            Player player = new Player();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            Root.Instance.Server.PlayerServerInfo[peerConnectedServerEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long nid = Root.Instance.NetworkEntityManager.AddEntity(player);
            
            Network.SendPacketToClients(new ServerSpawnPlayerPacket(nid, player.Position.X, player.Position.Y, player.Rotation));
            //TODO спавн союзников и других NetworkEntity?
            //TODO после спавна все включаем отображение (убираем экран о подключение). Мб спавн всех одним пакетом синхронизации.
        } 
        else if (currentWorld is BattleWorld)
        {
            Network.SendPacketToPeer(peerConnectedServerEvent.Id, new ServerWaitBattleEndPacket());
        }
        else
        {
            Log.Error($"Unknown world in Root.CurrentWorld: {currentWorld}");
        }
    }
    
    [EventListener]
    public void OnPeerDisconnectedServerEvent(PeerDisconnectedServerEvent peerDisconnectedServerEvent)
    {
        Log.Debug($"PeerDisconnectedServerEvent: {peerDisconnectedServerEvent.Id}");

        Player player = Root.Instance.Server.PlayerServerInfo[peerDisconnectedServerEvent.Id].Player;
        Root.Instance.NetworkEntityManager.RemoveEntity(player);
        Root.Instance.Server.PlayerServerInfo.Remove(peerDisconnectedServerEvent.Id);
        player.QueueFree();
    }
    
}