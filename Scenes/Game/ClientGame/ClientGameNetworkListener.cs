using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

public partial class ClientGame
{
	
	/*
	 * Сразу после подключения к серверу запускаем систему пинга.
	 * Все остальные действия (например, убрать загрузочный экран) проинициирует сервер через соответствующие пакеты.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		Log.Info($"Connected to server. My peer id = {Network.Multiplayer.GetUniqueId()}");
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
		Dictionary<SC_ChangeWorldPacket.ServerWorldType, PackedScene> worldScenesMap = new() 
		{
			{ SC_ChangeWorldPacket.ServerWorldType.Safe, ClientRoot.Instance.PackedScenes.SafeWorldMainScene },
			{ SC_ChangeWorldPacket.ServerWorldType.Battle, ClientRoot.Instance.PackedScenes.BattleWorldMainScene }
		};
		
		if (!worldScenesMap.TryGetValue(changeWorldPacket.WorldType, out var newWorldMainScene))
		{
			Log.Error($"Received unknown type of WorldMainScene: {changeWorldPacket.WorldType}");
			return;
		}
		
		ChangeMainScene(newWorldMainScene.Instantiate<IWorldMainScene>());
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
