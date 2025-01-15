using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

public partial class ServerGame
{
    
    /*
     * При подключении нового игрока создаем/активируем его PlayerProfile.
     * Если сейчас BattleWorld, то отправляемему заглушку и ждем окончания боя.
     * Если сейчас SafeWorld, то синхронизируем мир, загружая союзников и окружающие объекты. Союзникам передаем инфу о новом игроке.
     */
    [EventListener(ListenerSide.Server)]
    public void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
    {
        Log.Info($"Client connected to server. Peer id = {peerConnectedEvent.Id}");
        AddPlayerProfile(peerConnectedEvent.Id);

        if (World is ServerSafeWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));

            //Спауним нового игрока
            World.SpawnPlayer(PlayerProfilesById[peerConnectedEvent.Id]);
            
            //У нового игрока спауним всех остальных игроков
            foreach (ServerPlayer player in World.GetPlayersExcluding(peerConnectedEvent.Id))
            {
                Network.SendToClient(peerConnectedEvent.Id, new ClientAlly.SC_AllySpawnPacket(player.Nid, player.Position.X, player.Position.Y, player.Rotation, player.PlayerProfile.Id));
            }
            
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ClearLoadingScreenPacket());
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
    
    /*
     * При отключении игрока удаляем/деактивируем его PlayerProfile.
     * Удаляем игрока из мира и сообщаем об этом всем.
     */
    [EventListener(ListenerSide.Server)]
    public void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent)
    {
        ServerPlayer disconnectedPlayer = PlayerProfilesById[peerDisconnectedEvent.Id].Player;
        World.NetworkEntityManager.RemoveEntity(disconnectedPlayer); //TODO перенести куда-нибудь в NetworkEntityComponent?
        RemovePlayerProfile(peerDisconnectedEvent.Id);
        World.RemovePlayer(disconnectedPlayer);
        long nid = disconnectedPlayer.GetChild<NetworkEntityComponent>().Nid;
        disconnectedPlayer.QueueFree();
        
        //TODO Network.SendToAll(new ServerDestroyEntityPacket(nid));
    }
    
    /*
     * Игрок желает начать бой.
     * Ставим загрузочный экран, меняем текущий мир, создаем все объекты и сообщаем о них, убираем загрузочный экран. 
     */
    [EventListener(ListenerSide.Server)]
    public void OnWantToBattlePacket(CS_WantToBattlePacket wantToBattlePacket) 
    {
        //TODO
        /*Network.SendToAll(new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
        Network.SendToAll(new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle));
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        
        ChangeMainScene(serverBattleWorld);
        
        foreach (ServerPlayerProfile playerServerInfo in PlayerProfiles)
        {
            ServerPlayer player = World.CreateAndAddPlayer(PlayerProfilesById[playerServerInfo.Id]);
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));
            long newPlayerNid = World.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            Network.SendToAllExclude(playerServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
        }
        Network.SendToAll(new ClientGame.SC_ClearLoadingScreenPacket());*/
    }

    /*
     * Просто отвечаем на пакет пинга
     */
    [EventListener(ListenerSide.Server)]
    public void OnPingPacket(CS_PingPacket pingPacket) 
    {
        Network.SendToClient(pingPacket.SenderId, new PingChecker.SC_PingPacket(pingPacket.PingId));
    }
}
