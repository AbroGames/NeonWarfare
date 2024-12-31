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
	 * Сразу после подключения к серверу создаем профиль игрока и запускаем систему пинга 
	 * Все остальные действия (например, убрать загрузочный экран) проинициирует сервер через соответствующие пакеты.
	 */
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		AddPlayerProfile(Network.Multiplayer.GetUniqueId());
		PingChecker.Start();
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
