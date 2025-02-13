using System.Linq;
using NeonWarfare.Scenes.Game.ClientGame.Ping;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Game.ServerGame;

public partial class ServerGame
{
    
    /*
     * При подключении нового игрока создаем/активируем его PlayerProfile.
     */
    [EventListener(ListenerSide.Server)]
    public void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
    {
        Log.Info($"Client connected to server. Peer id = {peerConnectedEvent.Id}");
        AddPlayerProfile(peerConnectedEvent.Id);
    }
    
    /*
     * При отключении игрока удаляем/деактивируем его PlayerProfile.
     * Удаляем игрока из мира и сообщаем об этом всем.
     * Если это был последний игрок в боевом мире, то меняем мир на безопасный хаб.
     */
    [EventListener(ListenerSide.Server)]
    public void OnPeerDisconnectedEvent(PeerDisconnectedEvent peerDisconnectedEvent)
    {
        Log.Info($"Client disconnected from server. Peer id = {peerDisconnectedEvent.Id}");
        ServerPlayer disconnectedPlayer = PlayerProfilesByPeerId[peerDisconnectedEvent.Id].Player;
        disconnectedPlayer.QueueFree();
        
        RemovePlayerProfile(peerDisconnectedEvent.Id);
        Network.SendToAll(new ClientGame.ClientGame.SC_RemoveAllyProfilePacket(peerDisconnectedEvent.Id)); //Информацию об отключении отправляем всем, т.к. отключенный игрок уже отключен.

        if (PlayerProfiles.Count() == 0)
        {
            ServerSafeWorld serverSafeWorld = ServerRoot.Instance.PackedScenes.SafeWorld.Instantiate<ServerSafeWorld>();
            ChangeMainScene(serverSafeWorld);
        }
    }

    /*
     * После отправки игроком информации о себе, отправляем игроку его PlayerProfile, и профили союзников. Союзникам отправляем профиль игрока.
     * 
     * Если сейчас BattleWorld, то отправляем ему заглушку и ждем окончания боя.
     * Если сейчас SafeWorld, то синхронизируем мир, загружая союзных игроков и окружающие объекты. Союзникам передаем инфу о новом игроке.
     */
    [EventListener(ListenerSide.Server)]
    public void OnInitPlayerProfilePacket(CS_InitPlayerProfilePacket initPlayerProfilePacket)
    {
        long newPlayerPeerId = initPlayerProfilePacket.SenderId;
        ServerPlayerProfile newPlayerProfile = PlayerProfilesByPeerId[newPlayerPeerId];

        newPlayerProfile.Name = initPlayerProfilePacket.Name;
        newPlayerProfile.Color = initPlayerProfilePacket.Color;
        
        //Отправляем новому игроку его PlayerProfile
        Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_AddPlayerProfilePacket(newPlayerPeerId, initPlayerProfilePacket.Name, initPlayerProfilePacket.Color));
        //Отправляем союзникам PlayerProfile нового игрока
        Network.SendToAllExclude(newPlayerPeerId, new ClientGame.ClientGame.SC_AddAllyProfilePacket(newPlayerPeerId, initPlayerProfilePacket.Name, initPlayerProfilePacket.Color));
        
        //У нового игрока создаем профили уже подключенных игроков
        foreach (ServerPlayerProfile profile in GetPlayerProfilesExcluding(newPlayerPeerId))
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_AddAllyProfilePacket(profile.PeerId, profile.Name, profile.Color));
        }

        if (World is ServerSafeWorld)
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeWorldPacket(ClientGame.ClientGame.SC_ChangeWorldPacket.ServerWorldType.Safe));

            //Спауним нового игрока
            World.SpawnPlayerInCenter(PlayerProfilesByPeerId[newPlayerPeerId]);
            
            //У нового игрока спауним всех остальных игроков
            foreach (ServerPlayer player in GetPlayerProfilesExcluding(newPlayerPeerId).Select(profile => profile.Player))
            {
                Network.SendToClient(newPlayerPeerId, new ClientWorld.SC_AllySpawnPacket(player.Nid, player.Position.X, player.Position.Y, player.Rotation, player.PlayerProfile.Color, player.PlayerProfile.PeerId));
            }
            
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ClearLoadingScreenPacket());
        } 
        else if (World is ServerBattleWorld)
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.WAITING_END_OF_BATTLE));
        }
        else
        {
            Log.Error($"Unknown world in ServerGame.World: {World}");
        }
    }
    
    /*
     * Игрок желает начать бой.
     * Ставим загрузочный экран, меняем текущий мир, создаем все объекты и сообщаем о них, убираем загрузочный экран. 
     */
    [EventListener(ListenerSide.Server)]
    public void OnWantToBattlePacket(CS_WantToBattlePacket wantToBattlePacket) 
    {
        Network.SendToAll(new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
        Network.SendToAll(new ClientGame.ClientGame.SC_ChangeWorldPacket(ClientGame.ClientGame.SC_ChangeWorldPacket.ServerWorldType.Battle));

        ServerBattleWorld battleWorld = ServerRoot.Instance.PackedScenes.BattleWorld.Instantiate<ServerBattleWorld>();
        ChangeMainScene(battleWorld);
        
        foreach (ServerPlayerProfile playerProfile in PlayerProfiles)
        {
            battleWorld.SpawnPlayerInCenter(playerProfile);
        }
        
        Network.SendToAll(new ClientGame.ClientGame.SC_ClearLoadingScreenPacket());
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
