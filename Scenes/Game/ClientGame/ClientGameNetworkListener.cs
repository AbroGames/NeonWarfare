using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld.Server;

public partial class ClientGame
{
	
	[EventListener(ListenerSide.Client)]
	public static void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		Instance.ClearLoadingScreen(); //TODO в идеале вызывать только после синхронизации всех стартовых объектов (сервер должен отправить специальный пакет о том, что синхронизация закончена)
	}
	
	[EventListener(ListenerSide.Client)]
	public static void OnChangeWorldPacket(ChangeWorldPacket changeWorldPacket)
	{
		Instance.NetworkEntityManager.Clear(); //TODO подумать над тем, чтобы перенести его в ClientWorld, чтобы он очищался гарантировано и вовремя

		PackedScene newWorldMainScene = changeWorldPacket.WorldType switch //TODO in enum map in packet?
		{
			ChangeWorldPacket.ServerWorldType.Safe => ClientRoot.Instance.PackedScenes.Client.GameMainScenes.SafeWorld,
			ChangeWorldPacket.ServerWorldType.Battle => ClientRoot.Instance.PackedScenes.Client.GameMainScenes.BattleWorld,
			_ => null
		};

		if (newWorldMainScene == null)
		{
			Log.Error($"Received unknown type of WorldMainScene: {changeWorldPacket.WorldType}");
			return;
		}

		Instance.ChangeMainScene(newWorldMainScene.Instantiate<IWorldMainScene>());
	}
	
	[EventListener(ListenerSide.Client)]
	public static void OnWaitBattleEndPacket(WaitBattleEndPacket emptyPacket)
	{
		Instance.SetLoadingScreen(ClientRoot.Instance.PackedScenes.Client.Screens.WaitingForBattleEndCanvas.Instantiate<CanvasLayer>());
	}
}
