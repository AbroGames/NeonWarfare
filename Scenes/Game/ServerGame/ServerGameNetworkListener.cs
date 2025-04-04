using System.Linq;
using NeonWarfare.Scenes.Game.ClientGame.Ping;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
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
        if (!World.PlayersByPeerId.TryGetValue(peerDisconnectedEvent.Id, out ServerPlayer disconnectedPlayer))
        {
            RemovePlayerProfile(peerDisconnectedEvent.Id);
            return;
        }
        
        if (disconnectedPlayer.IsValid())
        {
            disconnectedPlayer.QueueFree();
            BroadcastMessage($"{disconnectedPlayer.PlayerProfile.Name} disconnected.");
        }
        
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

        if (PlayerProfiles.Any(existingPlayer => existingPlayer.Name == initPlayerProfilePacket.Name))
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_DisconnectedFromServerPacket("Unable to connect to the server", "Player with this name already connected."));
            Log.Warning($"Player with name '{initPlayerProfilePacket.Name}' just had tried to connect, while another player with this name is already connected.");

            var timer = GetTree().CreateTimer(0.1, ignoreTimeScale: true);
            timer.Timeout += () =>
            {
                Network.Api.DisconnectPeer((int)newPlayerPeerId);
            };
            return;
        }

        newPlayerProfile.Name = initPlayerProfilePacket.Name;
        newPlayerProfile.Color = initPlayerProfilePacket.Color;
        newPlayerProfile.IsAdmin = ServerRoot.Instance.CmdParams.Admin == newPlayerProfile.Name;
        
        //Отправляем новому игроку настройки мира
        Network.SendToClient(newPlayerPeerId, GameSettings.ToPacket());
        //Отправляем новому игроку его PlayerProfile
        Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_AddPlayerProfilePacket(newPlayerPeerId, initPlayerProfilePacket.Name, initPlayerProfilePacket.Color, newPlayerProfile.IsAdmin));
        //Отправляем союзникам PlayerProfile нового игрока
        Network.SendToAllExclude(newPlayerPeerId, new ClientGame.ClientGame.SC_AddAllyProfilePacket(newPlayerPeerId, initPlayerProfilePacket.Name, initPlayerProfilePacket.Color));
        //Отправляем всем инфу о характеристиках нового игрока
        Network.SendToAll(new ClientAllyProfile.SC_ChangeAllyProfilePacket(newPlayerPeerId, newPlayerProfile.MaxHp, newPlayerProfile.RegenHpSpeed, newPlayerProfile.MovementSpeed, newPlayerProfile.RotationSpeed));
        //Отправляем игроку его скиллы
        foreach (var kv in newPlayerProfile.SkillById)
        {
            Network.SendToClient(newPlayerPeerId, new ClientPlayerProfile.SC_ChangeSkillPlayerProfilePacket(kv.Key, kv.Value.SkillType, kv.Value.Cooldown));
        }
        
        //У нового игрока создаем профили уже подключенных игроков
        var profiles = GetPlayerProfilesExcluding(newPlayerPeerId);
        foreach (ServerPlayerProfile profile in profiles)
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_AddAllyProfilePacket(profile.PeerId, profile.Name, profile.Color));
            Network.SendToClient(newPlayerPeerId, new ClientAllyProfile.SC_ChangeAllyProfilePacket(profile.PeerId, profile.MaxHp, profile.RegenHpSpeed, profile.MovementSpeed, profile.RotationSpeed));
        }

        if (World.GetServerWorldType() == WorldInfoStorage.WorldType.Safe)
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.LOADING));
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeWorldPacket(World.GetServerWorldType()));

            //Спауним нового игрока
            World.SpawnPlayerInCenter(PlayerProfilesByPeerId[newPlayerPeerId]);
            
            //У нового игрока спауним всех остальных игроков
            foreach (ServerPlayer player in World.GetPlayerProfilesExcluding(newPlayerPeerId))
            {
                Network.SendToClient(newPlayerPeerId, new ClientWorld.SC_AllySpawnPacket(player.Nid, player.Position, player.Rotation, player.PlayerProfile.Color, player.PlayerProfile.PeerId));
            }
            
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ClearLoadingScreenPacket());
            Network.SendToAll(new ClientGame.ClientGame.SC_UpdateReadyClientsListPacket(((ServerSafeWorld)World).ReadyClients.ToArray()));
        } 
        else if (World.GetServerWorldType() == WorldInfoStorage.WorldType.Battle)
        {
            Network.SendToClient(newPlayerPeerId, new ClientGame.ClientGame.SC_ChangeLoadingScreenPacket(LoadingScreenBuilder.LoadingScreenType.WAITING_END_OF_BATTLE));
        }
        else
        {
            Log.Error($"Unknown world in ServerGame.World: {World}");
        }
        
        BroadcastMessage($"[color={newPlayerProfile.Color.ToHtml()}]{newPlayerProfile.Name}[/color] connected.");
    }
    
    /*
     * Игрок желает начать бой.
     * Ставим загрузочный экран, меняем текущий мир, создаем все объекты и сообщаем о них, убираем загрузочный экран. 
     */
    [EventListener(ListenerSide.Server)]
    public void OnWantToBattlePacket(CS_WantToBattlePacket wantToBattlePacket) 
    {
        if (World is not ServerSafeWorld safeWorld)
        {
            return;
        }

        string messageReadyPart;
        if (wantToBattlePacket.WantToBattle)
        {
            safeWorld.ReadyClients.Add(wantToBattlePacket.SenderId);
            messageReadyPart = $"[color=green]READY[/color]";
        }
        else
        {
            safeWorld.ReadyClients.Remove(wantToBattlePacket.SenderId);
            messageReadyPart = $"[color=red]NOT READY[/color]";
        }

        var player = PlayerProfilesByPeerId[wantToBattlePacket.SenderId];
        BroadcastMessage($"[color={player.Color.ToHtml()}]{player.Name}[/color] are {messageReadyPart} to battle.");

        if (safeWorld.ReadyClients.Count == PlayerProfiles.Count())
        {
            GoToBattleWorld();
            return;
        }
        
        Network.SendToAll(new ClientGame.ClientGame.SC_UpdateReadyClientsListPacket(safeWorld.ReadyClients.ToArray()));
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnAdminGoToBattlePacket(CS_AdminGoToBattlePacket wantToBattlePacket) 
    {
        if(PlayerProfilesByPeerId[wantToBattlePacket.SenderId].IsAdmin)
            GoToBattleWorld();
    }

    /*
     * Просто отвечаем на пакет пинга
     */
    [EventListener(ListenerSide.Server)]
    public void OnPingPacket(CS_PingPacket pingPacket) 
    {
        Network.SendToClient(pingPacket.SenderId, new PingChecker.SC_PingPacket(pingPacket.PingId));
    }

    [EventListener(ListenerSide.Server)]
    public void OnAchievementPacket(CS_ClientUnlockedAchievementPacket achievementPacket)
    {
        Network.SendToAll(new ClientGame.ClientGame.SC_ClientUnlockedAchievementBroadcastPacket(achievementPacket.SenderId, achievementPacket.AchievementId));
    }
    
    [EventListener(ListenerSide.Server)]
    public void OnClientRequestedSelfResurrection(CS_ClientRequestedSelfResurrection selfResurrectionPacket)
    {
        ServerRoot.Instance.Game.World.PlayersByPeerId[selfResurrectionPacket.SenderId].OnResurrectRequest(100);
    }
}
