using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Client;

namespace NeonWarfare.NetOld.Server;

[GameService]
public class GodotServerService
{

    [EventListener]
    public void OnPeerConnectedServerEvent(PeerConnectedServerEvent peerConnectedServerEvent)
    {
        Log.Debug($"PeerConnectedServerEvent: {peerConnectedServerEvent.Id}");
        
        PlayerServerInfo newPlayerServerInfo = new PlayerServerInfo(peerConnectedServerEvent.Id);
        ServerRoot.Instance.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        Netplay.Send(peerConnectedServerEvent.Id,
            new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = Root.Instance.CurrentWorld;
        if (currentWorld is SafeWorld)
        {
            Player player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            ServerRoot.Instance.Server.PlayerServerInfo[peerConnectedServerEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = Root.Instance.NetworkEntityManager.AddEntity(player);

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

            Netplay.Send(peerConnectedServerEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in ServerRoot.Instance.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = Root.Instance.NetworkEntityManager.GetNid(ally);
                Netplay.Send(peerConnectedServerEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                Netplay.Send(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
            
            //TODO после спавна все включаем отображение (убираем экран о подключение). Мб спавн всех одним пакетом синхронизации.
        } 
        else if (currentWorld is BattleWorld)
        {
            Netplay.Send(peerConnectedServerEvent.Id, new ServerWaitBattleEndPacket());
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

        Player player = ServerRoot.Instance.Server.PlayerServerInfo[peerDisconnectedServerEvent.Id].Player;
        Root.Instance.NetworkEntityManager.RemoveEntity(player);
        ServerRoot.Instance.Server.PlayerServerInfo.Remove(peerDisconnectedServerEvent.Id);
        long nid = Root.Instance.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Netplay.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
}