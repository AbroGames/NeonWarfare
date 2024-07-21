using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Client;

namespace NeonWarfare.NetOld.Server;

public static class GodotServerService
{

    [EventListener(ListenerSide.Server)]
    public static void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
    {
        PlayerServerInfo newPlayerServerInfo = new PlayerServerInfo(peerConnectedEvent.Id);
        ServerRoot.Instance.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        Netplay.Send(peerConnectedEvent.Id, new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = ServerRoot.Instance.Game.World;
        if (currentWorld is ServerSafeWorld)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            ServerRoot.Instance.Server.PlayerServerInfo[peerConnectedEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = ServerRoot.Instance.Game.NetworkEntityManager.AddEntity(player);

            if (currentWorld.GetChild<Camera>() == null)
            {
                var camera = new Camera(); //TODO del from server!!
                camera.Position = player.Position;
                camera.TargetNode = player;
                camera.Zoom = Vec(0.65);
                camera.SmoothingPower = 1.5;
                currentWorld.AddChild(camera);
                camera.Enabled = true;
            }

            Netplay.Send(peerConnectedEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in ServerRoot.Instance.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = ServerRoot.Instance.Game.NetworkEntityManager.GetNid(ally);
                Netplay.Send(peerConnectedEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                Netplay.Send(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
            
            //TODO после спавна все включаем отображение (убираем экран о подключение). Мб спавн всех одним пакетом синхронизации.
        } 
        else if (currentWorld is ServerBattleWorld)
        {
            Netplay.Send(peerConnectedEvent.Id, new ServerWaitBattleEndPacket());
        }
        else
        {
            Log.Error($"Unknown world in Root.CurrentWorld: {currentWorld}");
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent)
    {
        Player player = ServerRoot.Instance.Server.PlayerServerInfo[peerDisconnectedEvent.Id].Player;
        ServerRoot.Instance.Game.NetworkEntityManager.RemoveEntity(player);
        ServerRoot.Instance.Server.PlayerServerInfo.Remove(peerDisconnectedEvent.Id);
        long nid = ServerRoot.Instance.Game.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Netplay.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
}