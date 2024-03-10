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
        
        PlayerServerInfo newPlayerServerInfo = new PlayerServerInfo(peerConnectedServerEvent.Id);
        Root.Instance.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        Network.SendPacketToPeer(peerConnectedServerEvent.Id,
            new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = Root.Instance.CurrentWorld;
        if (currentWorld is SafeWorld)
        {
            Player player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            Root.Instance.Server.PlayerServerInfo[peerConnectedServerEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = Root.Instance.NetworkEntityManager.AddEntity(player);
            
            var camera = new Camera(); //TODO del from server!!
            camera.Position = player.Position;
            camera.TargetNode = player;
            camera.Zoom = Vec(0.65);
            camera.SmoothingPower = 1.5;
            currentWorld.AddChild(camera);
            camera.Enabled = true;
            
            Network.SendPacketToPeer(peerConnectedServerEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = Root.Instance.NetworkEntityManager.GetNid(ally);
                Network.SendPacketToPeer(peerConnectedServerEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                Network.SendPacketToPeer(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
            
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
        long nid = Root.Instance.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Network.SendPacketToClients(new ServerDestroyEntityPacket(nid));
    }
    
}