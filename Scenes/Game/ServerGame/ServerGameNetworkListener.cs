using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

public partial class ServerGame
{
    
    [EventListener(ListenerSide.Server)]
    public static void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent) //TODO refactor this method
    {
        PlayerServerInfo newPlayerServerInfo = new PlayerServerInfo(peerConnectedEvent.Id);
        Instance.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        Network.SendToClient(peerConnectedEvent.Id, new ChangeWorldPacket(ChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = Instance.World;
        if (currentWorld is ServerSafeWorld)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            Instance.Server.PlayerServerInfo[peerConnectedEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = Instance.World.NetworkEntityManager.AddEntity(player);

            Network.SendToClient(peerConnectedEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in Instance.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = Instance.World.NetworkEntityManager.GetNid(ally);
                Network.SendToClient(peerConnectedEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                Network.SendToClient(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
            
            //TODO после спавна все включаем отображение (убираем экран о подключение). Мб спавн всех одним пакетом синхронизации.
        } 
        else if (currentWorld is ServerBattleWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new WaitBattleEndPacket());
        }
        else
        {
            Log.Error($"Unknown world in Root.CurrentWorld: {currentWorld}");
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent) //TODO refactor this method
    {
        Player player = Instance.Server.PlayerServerInfo[peerDisconnectedEvent.Id].Player;
        Instance.World.NetworkEntityManager.RemoveEntity(player);
        Instance.Server.PlayerServerInfo.Remove(peerDisconnectedEvent.Id);
        long nid = Instance.World.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Network.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnToBattleButtonClickPacket(ToBattleButtonClickPacket emptyPacket) //TODO refactor this method
    {
        Network.SendToAll(new ChangeWorldPacket(ChangeWorldPacket.ServerWorldType.Battle));
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        
        Instance.ChangeMainScene(serverBattleWorld);
        //TODO Instance.NetworkEntityManager.Clear(); в этом нет смысла, т.к. менеджер перенесен в World. Удалить.
        
        foreach (PlayerServerInfo playerServerInfo in Instance.Server.PlayerServerInfo.Values)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            playerServerInfo.Player = player;
            serverBattleWorld.AddChild(player);
            long newPlayerNid = Instance.World.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (PlayerServerInfo allyServerInfo in Instance.Server.PlayerServerInfo.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                Network.SendToClient(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
    }
}
