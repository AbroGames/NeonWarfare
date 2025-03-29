using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scenes.Screen.SafeHud;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.GameSettings;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame
{
	[EventListener(ListenerSide.Server)]
	public void OnServerDisconnectedEvent(ServerDisconnectedEvent peerConnectedEvent)
	{
		if (CurrentGameState is GameState.Disconnecting or GameState.None)
			return;
		
		ExitToDisconnectedScreen(new SC_DisconnectedFromServerPacket("Disconnected from server", "Unknown reason"));
	}
	
	/*
	 * Сразу после подключения к серверу запускаем систему пинга и отправляем пакет с информацией о себе.
	 * Все остальные действия (например, убрать загрузочный экран) проинициирует сервер через соответствующие пакеты.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		Log.Info($"Connected to server. My peer id = {Network.Multiplayer.GetUniqueId()}");
		
		Settings playerSettings = ClientRoot.Instance.Settings;
		CurrentGameState = GameState.Connecting;
		Network.SendToServer(new ServerGame.ServerGame.CS_InitPlayerProfilePacket(playerSettings.PlayerName, playerSettings.PlayerColor));
		PingChecker.Start();
	}
	
	/*
	 * Вызывается при подключении игрока к серверу.
	 * Создаем для нас PlayerProfile.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnAddPlayerProfilePacket(SC_AddPlayerProfilePacket addPlayerProfilePacket)
	{
		CurrentGameState = GameState.SynchronizingStates;
		Log.Info($"Create PlayerProfile. Peer id = {addPlayerProfilePacket.PeerId}");
		
		AddPlayerProfile(addPlayerProfilePacket.PeerId);
		PlayerProfile.Name = addPlayerProfilePacket.Name;
		PlayerProfile.Color = addPlayerProfilePacket.Color;
		PlayerProfile.IsAdmin = addPlayerProfilePacket.IsAdmin;
	}
	
	/*
	 * Вызывается при подключении другого игрока к серверу.
	 * Создаем для него AllyProfile.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnAddAllyProfilePacket(SC_AddAllyProfilePacket addAllyProfilePacket)
	{
		Log.Info($"Create AllyProfile. Peer id = {addAllyProfilePacket.PeerId}");
		
		AddAllyProfile(addAllyProfilePacket.PeerId);
		AllyProfilesByPeerId[addAllyProfilePacket.PeerId].Name = addAllyProfilePacket.Name;
		AllyProfilesByPeerId[addAllyProfilePacket.PeerId].Color = addAllyProfilePacket.Color;
	}
	
	/*
	 * Вызывается при отключении другого игрока от сервера.
	 * Удаляем его AllyProfile.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnRemoveAllyProfilePacket(SC_RemoveAllyProfilePacket removeAllyProfilePacket)
	{
		Log.Info($"Remove AllyProfile. Peer id = {removeAllyProfilePacket.PeerId}");
		RemoveAllyProfile(removeAllyProfilePacket.PeerId);
	}
	
	/*
	 * Создаем новый экземпляр WorldMainScene и делаем его активным, текущий удаляем.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnChangeWorldPacket(SC_ChangeWorldPacket changeWorldPacket)
	{
		Log.Info($"Change MainScene. WorldType = {changeWorldPacket.WorldType}");
		PackedScene worldMainScene = WorldInfoStorage.GetClientMainScene(changeWorldPacket.WorldType);
		ChangeMainScene(worldMainScene.Instantiate<IWorldMainScene>());
	}
	
	/*
	 * Устанавливаем/меняем загрузочный экран
	 */
	[EventListener(ListenerSide.Client)]
	public void OnChangeLoadingScreenPacket(SC_ChangeLoadingScreenPacket changeLoadingScreenPacket)
	{
		if (changeLoadingScreenPacket.LoadingScreenType == LoadingScreenBuilder.LoadingScreenType.WAITING_END_OF_BATTLE)
		{
			ClientRoot.Instance.UnlockAchievement(AchievementIds.NopeAchievement);
			CurrentGameState = GameState.WaitingForBattleEnd;
		}

		ClientRoot.Instance.SetLoadingScreen(changeLoadingScreenPacket.LoadingScreenType);
	}
	
	/*
	 * Отключаем загрузочный экран
	 */
	[EventListener(ListenerSide.Client)]
	public void OnClearLoadingScreenPacket(SC_ClearLoadingScreenPacket clearLoadingScreenPacket)
	{
		CurrentGameState = GameState.Playing;
		ClientRoot.Instance.ClearLoadingScreen();
	}
	
	/*
	 * Получаем с сервера все настройки мира
	 */
	[EventListener(ListenerSide.Client)]
	public void OnChangeSettingsPacket(SC_ChangeSettingsPacket changeSettingsPacket)
	{
		GameSettings = GameSettings.FromPacket(changeSettingsPacket);
	}

	[EventListener(ListenerSide.Client)]
	public void OnClientUnlockedAchievementBroadcastPacket(SC_ClientUnlockedAchievementBroadcastPacket packet)
	{
		var achievement = AchievementsList.GetAchievement(packet.AchievementId);
		if (achievement is null)
		{
			Log.Warning($"Received achievement notification with non-existent ID: {packet.AchievementId}");
			return;
		}

		ClientAllyProfile profile;
		if (packet.UnlockingPeer == PlayerProfile.PeerId)
		{
			profile = PlayerProfile;
		}
		else
		{
			_allyProfilesByPeerId.TryGetValue(packet.UnlockingPeer, out profile);
		}

		if (profile is null)
		{
			Log.Warning($"Attempt to unlock achievement for peer with non-existent ID: {packet.UnlockingPeer}");
			return;
		}
		
		var msg = new ChatMessage($"[color={profile.Color.ToHtml()}]{profile.Name}[/color] has just unlocked the achievement: " +
		                          $"[color={achievement.Color.ToHtml()}][hint={achievement.Description}][lb]{achievement.Name}[rb][/hint][/color]", SenderInfo.System);
		Hud.ChatContainer.ReceiveMessage(msg);
	}

	[EventListener(ListenerSide.Client)]
	public void OnUpdateReadyClientsList(SC_UpdateReadyClientsListPacket updateReadyClientsListPacket)
	{
		if (Hud is not SafeHud safeHud)
			return;
		
		safeHud.ReadyPlayersList.RebuildReadyPlayersList(updateReadyClientsListPacket.ReadyClients);
	}

	[EventListener(ListenerSide.Client)]
	public void OnDisconnectedFromServer(SC_DisconnectedFromServerPacket disconnectPacket)
	{
		CurrentGameState = GameState.Disconnecting;
		ExitToDisconnectedScreen(disconnectPacket);
	}
}
