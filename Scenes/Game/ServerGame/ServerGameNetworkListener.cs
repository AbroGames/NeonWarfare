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
     * Если сейчас BattleWorld, то отправляем ему заглушку и ждем окончания боя.
     * Если сейчас SafeWorld, то синхронизируем мир, загружая союзников и окружающие объекты. Союзникам передаем инфу о новом игроке.
     */
    [EventListener(ListenerSide.Server)]
    public void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
    {
        Log.Info($"Client connected to server. Peer id = {peerConnectedEvent.Id}");
        
        AddPlayerProfile(peerConnectedEvent.Id);
        //У нового игрока создаем профили уже подключенных игроков
        foreach (ServerPlayerProfile profile in GetPlayerProfilesExcluding(peerConnectedEvent.Id))
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_AddAllyProfilePacket(profile.PeerId));
        }

        if (World is ServerSafeWorld)
        {
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
            Network.SendToClient(peerConnectedEvent.Id, new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));

            //Спауним нового игрока
            World.SpawnPlayer(PlayerProfilesByPeerId[peerConnectedEvent.Id]);
            
            //У нового игрока спауним всех остальных игроков
            foreach (ServerPlayer player in GetPlayerProfilesExcluding(peerConnectedEvent.Id).Select(profile => profile.Player))
            {
                Network.SendToClient(peerConnectedEvent.Id, new ClientWorld.SC_AllySpawnPacket(player.Nid, player.Position.X, player.Position.Y, player.Rotation, player.PlayerProfile.PeerId));
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
        Log.Info($"Client disconnected from server. Peer id = {peerDisconnectedEvent.Id}");
        ServerPlayer disconnectedPlayer = PlayerProfilesByPeerId[peerDisconnectedEvent.Id].Player;
        disconnectedPlayer.QueueFree();
        RemovePlayerProfile(peerDisconnectedEvent.Id);
    }
    
    /*
     * Игрок желает начать бой.
     * Ставим загрузочный экран, меняем текущий мир, создаем все объекты и сообщаем о них, убираем загрузочный экран. 
     */
    [EventListener(ListenerSide.Server)]
    public void OnWantToBattlePacket(CS_WantToBattlePacket wantToBattlePacket) 
    {
        Network.SendToAll(new ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
        
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        ChangeMainScene(serverBattleWorld);
        Network.SendToAll(new ClientGame.SC_ChangeWorldPacket(ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle));
        
        foreach (ServerPlayerProfile playerProfile in PlayerProfiles)
        {
            serverBattleWorld.SpawnPlayer(playerProfile);
        }
        
        Network.SendToAll(new ClientGame.SC_ClearLoadingScreenPacket());
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
