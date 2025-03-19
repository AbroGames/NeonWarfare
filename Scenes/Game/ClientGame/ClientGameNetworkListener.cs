using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.ClientSettings;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.PlayerSettings;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame
{
	
	/*
	 * Сразу после подключения к серверу запускаем систему пинга и отправляем пакет с информацией о себе.
	 * Все остальные действия (например, убрать загрузочный экран) проинициирует сервер через соответствующие пакеты.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		Log.Info($"Connected to server. My peer id = {Network.Multiplayer.GetUniqueId()}");
		
		Settings playerSettings = ClientRoot.Instance.Settings;
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
		Log.Info($"Create PlayerProfile. Peer id = {addPlayerProfilePacket.PeerId}");
		
		AddPlayerProfile(addPlayerProfilePacket.PeerId);
		PlayerProfile.Name = addPlayerProfilePacket.Name;
		PlayerProfile.Color = addPlayerProfilePacket.Color;
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
		ChangeMainScene(changeWorldPacket.WorldMainScene.Instantiate<IWorldMainScene>());
	}
	
	/*
	 * Устанавливаем/меняем загрузочный экран
	 */
	[EventListener(ListenerSide.Client)]
	public void OnChangeLoadingScreenPacket(SC_ChangeLoadingScreenPacket changeLoadingScreenPacket)
	{
		SetLoadingScreen(changeLoadingScreenPacket.LoadingScreenType);
	}
	
	/*
	 * Отключаем загрузочный экран
	 */
	[EventListener(ListenerSide.Client)]
	public void OnClearLoadingScreenPacket(SC_ClearLoadingScreenPacket clearLoadingScreenPacket)
	{
		ClearLoadingScreen();
	}
}
