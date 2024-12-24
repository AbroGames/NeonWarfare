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
        
        Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));

        Node currentWorld = World;
        if (currentWorld is ServerSafeWorld)
        {
            Player player = ServerRoot.Instance.PackedScenes.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            PlayerProfiles[peerConnectedEvent.Id].Player = player;
            currentWorld.AddChild(player);
            long newPlayerNid = World.NetworkEntityManager.AddEntity(player);

            Network.SendToClient(peerConnectedEvent.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            
            foreach (ServerPlayerProfile playerServerInfo in PlayerProfiles.Values)
            {
                if (playerServerInfo.Id == peerConnectedEvent.Id) continue;
                
                Player ally = playerServerInfo.Player;
                long allyNid = World.NetworkEntityManager.GetNid(ally);
                Network.SendToClient(peerConnectedEvent.Id, new ServerSpawnAllyPacket(allyNid, ally.Position.X, ally.Position.Y, ally.Rotation));
                Network.SendToClient(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        } 
        else if (currentWorld is ServerBattleWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.WAITING_END_OF_BATTLE));
        }
        else
        {
            Log.Error($"Unknown world in Root.CurrentWorld: {currentWorld}");
        }
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent)
    {
        Player player = PlayerProfiles[peerDisconnectedEvent.Id].Player;
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
        
        foreach (ServerPlayerProfile playerServerInfo in PlayerProfiles.Values)
        {
            Player player = ServerRoot.Instance.PackedScenes.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            playerServerInfo.Player = player;
            serverBattleWorld.AddChild(player);
            long newPlayerNid = World.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (ServerPlayerProfile allyServerInfo in PlayerProfiles.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                Network.SendToClient(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
        Network.SendToAll(new ClientGame.SC_ClearLoadingScreenPacket());
    }

    [EventListener(ListenerSide.Server)]
    public void OnPingPacket(CS_PingPacket pingPacket) 
    {
        Network.SendToClient(pingPacket.SenderId, new PingChecker.SC_PingPacket(pingPacket.PingId));
    }
}
