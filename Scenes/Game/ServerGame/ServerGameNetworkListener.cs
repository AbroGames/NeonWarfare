using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

public partial class ServerGame
{
    
    [EventListener(ListenerSide.Server)]
    public void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
    {
        AddPlayerProfile(peerConnectedEvent.Id);

        if (World is ServerSafeWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));
            
            Player player = ServerRoot.Instance.PackedScenes.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            PlayerProfilesById[peerConnectedEvent.Id].Player = player;
            World.AddChild(player);
            long newPlayerNid = World.NetworkEntityManager.AddEntity(player);

            //У нового игрока спауним его самого
            Network.SendToClient(peerConnectedEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //У всех остальных игроков спауним нового игрока
            Network.SendToAllExclude(peerConnectedEvent.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            //У нового игрока спауним всех остальных игроков
            var allyProfiles = GetPlayerProfilesExcluding(peerConnectedEvent.Id);
            foreach (ServerPlayerProfile playerServerInfo in allyProfiles)
            {
                Player ally = playerServerInfo.Player;
                long allyNid = World.NetworkEntityManager.GetNid(ally);
                Network.SendToClient(peerConnectedEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
            }
        } 
        else if (World is ServerBattleWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.WAITING_END_OF_BATTLE));
        }
        else
        {
            Log.Error($"Unknown world in ServerGame.World: {World}");
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent)
    {
        Player player = PlayerProfilesById[peerDisconnectedEvent.Id].Player;
        World.NetworkEntityManager.RemoveEntity(player);
        RemovePlayerProfile(peerDisconnectedEvent.Id);
        long nid = World.NetworkEntityManager.RemoveEntity(player);
        player.QueueFree();
        
        Network.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnWantToBattlePacket(CS_WantToBattlePacket emptyPacket) 
    {
        Network.SendToAll(new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
        Network.SendToAll(new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle));
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        
        ChangeMainScene(serverBattleWorld);
        
        foreach (ServerPlayerProfile playerServerInfo in PlayerProfiles)
        {
            Player player = ServerRoot.Instance.PackedScenes.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            playerServerInfo.Player = player;
            serverBattleWorld.AddChild(player);
            long newPlayerNid = World.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            Network.SendToAllExclude(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
        }
        Network.SendToAll(new ClientGame.SC_ClearLoadingScreenPacket());
    }

    [EventListener(ListenerSide.Server)]
    public void OnPingPacket(CS_PingPacket pingPacket) 
    {
        Network.SendToClient(pingPacket.SenderId, new PingChecker.SC_PingPacket(pingPacket.PingId));
    }
}
