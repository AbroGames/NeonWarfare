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
	 * Сразу после подключения к серверу создаем профиль игрока и запускаем систему пинга.
	 * Все остальные действия (например, убрать загрузочный экран) проинициирует сервер через соответствующие пакеты.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		long myPeerId = Network.Multiplayer.GetUniqueId();
		Log.Info($"Connected to server. My peer id = {myPeerId}");
		AddPlayerProfile(myPeerId);
		PingChecker.Start();
	}
	
	/*
	 * Вызывается при подключении другого игрока к серверу.
	 * Создаем для него PlayerProfile.
	 */
	//TODO будет надежней, если AllyProfile будут создаваться и удаляться по команде с сервера. Серверу все равно инициализировать там всю инфу.
	//Мб даже наш PlayerProfile. Отдельным пакетом, с нашим peerId. Тогда все будет аналогично инициализации Players/Allies
	[EventListener(ListenerSide.Client)]
	public void OnPeerConnectedEvent(PeerConnectedEvent peerConnectedEvent)
	{
		if (peerConnectedEvent.Id == 1) return; //Если пиром является сервер, то ничего не делаем.
		
		Log.Info($"New client connected to server. Peer id = {peerConnectedEvent.Id}");
		AddAllyProfile(peerConnectedEvent.Id);
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
