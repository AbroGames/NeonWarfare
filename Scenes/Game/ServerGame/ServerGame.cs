using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld;
using NeonWarfare.NetOld.Server;

public partial class ServerGame : Node2D
{

    private static ServerGame Game => ServerRoot.Instance.Game;
	
	public override void _Ready()
	{
		InitNetwork();
		ChangeMainScene(new ServerSafeWorld());
	}
    
    [EventListener]
    public static void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent) //TODO refactor this method
    {
        PlayerServerInfo newPlayerServerInfo = new PlayerServerInfo(peerConnectedEvent.Id);
        Game.Server.PlayerServerInfo.Add(newPlayerServerInfo.Id, newPlayerServerInfo);
        
        Network.SendToClient(peerConnectedEvent.Id, new ChangeWorldPacket(ChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = Game.World;
        if (currentWorld is ServerSafeWorld)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            Game.Server.PlayerServerInfo[peerConnectedEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = Game.NetworkEntityManager.AddEntity(player);

            Network.SendToClient(peerConnectedEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //TODO спавн союзников и других NetworkEntity?
            foreach (PlayerServerInfo playerServerInfo in Game.Server.PlayerServerInfo.Values)
            {
                if (playerServerInfo.Id == newPlayerServerInfo.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = Game.NetworkEntityManager.GetNid(ally);
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
    
    [EventListener]
    public static void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent) //TODO refactor this method
    {
        Player player = Game.Server.PlayerServerInfo[peerDisconnectedEvent.Id].Player;
        Game.NetworkEntityManager.RemoveEntity(player);
        Game.Server.PlayerServerInfo.Remove(peerDisconnectedEvent.Id);
        long nid = Game.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Network.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
    [EventListener]
    public static void OnToBattleButtonClickPacket(ToBattleButtonClickPacket emptyPacket) //TODO refactor this method
    {
        Network.SendToAll(new ChangeWorldPacket(ChangeWorldPacket.ServerWorldType.Battle));
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        
        Game.ChangeMainScene(serverBattleWorld);
        Game.NetworkEntityManager.Clear();
        
        foreach (PlayerServerInfo playerServerInfo in Game.Server.PlayerServerInfo.Values)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            playerServerInfo.Player = player;
            serverBattleWorld.AddChild(player);
            long newPlayerNid = Game.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (PlayerServerInfo allyServerInfo in Game.Server.PlayerServerInfo.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                Network.SendToClient(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
    }
}
