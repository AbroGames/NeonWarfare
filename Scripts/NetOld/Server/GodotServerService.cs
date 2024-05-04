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
        NeonWarfare.ServerRoot.Instance.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        NetworkOld.SendPacketToPeer(peerConnectedServerEvent.Id,
            new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = NeonWarfare.Root.Instance.CurrentWorld;
        if (currentWorld is NeonWarfare.SafeWorld)
        {
            NeonWarfare.Player player = NeonWarfare.Root.Instance.PackedScenes.World.Player.Instantiate<NeonWarfare.Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            NeonWarfare.ServerRoot.Instance.Server.PlayerServerInfo[peerConnectedServerEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = NeonWarfare.Root.Instance.NetworkEntityManager.AddEntity(player);

            if (currentWorld.GetChild<NeonWarfare.Camera>() == null)
            {
                var camera = new NeonWarfare.Camera(); //TODO del from server!!
                camera.Position = player.Position;
                camera.TargetNode = player;
                camera.Zoom = Vec(0.65);
                camera.SmoothingPower = 1.5;
                currentWorld.AddChild(camera);
                camera.Enabled = true;
            }

            NetworkOld.SendPacketToPeer(peerConnectedServerEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in NeonWarfare.ServerRoot.Instance.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                NeonWarfare.Player ally = playerServerInfo.Player;
                long allyNid = NeonWarfare.Root.Instance.NetworkEntityManager.GetNid(ally);
                NetworkOld.SendPacketToPeer(peerConnectedServerEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                NetworkOld.SendPacketToPeer(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
            
            //TODO после спавна все включаем отображение (убираем экран о подключение). Мб спавн всех одним пакетом синхронизации.
        } 
        else if (currentWorld is NeonWarfare.BattleWorld)
        {
            NetworkOld.SendPacketToPeer(peerConnectedServerEvent.Id, new ServerWaitBattleEndPacket());
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

        NeonWarfare.Player player = NeonWarfare.ServerRoot.Instance.Server.PlayerServerInfo[peerDisconnectedServerEvent.Id].Player;
        NeonWarfare.Root.Instance.NetworkEntityManager.RemoveEntity(player);
        NeonWarfare.ServerRoot.Instance.Server.PlayerServerInfo.Remove(peerDisconnectedServerEvent.Id);
        long nid = NeonWarfare.Root.Instance.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        NetworkOld.SendPacketToClients(new ServerDestroyEntityPacket(nid));
    }
    
}