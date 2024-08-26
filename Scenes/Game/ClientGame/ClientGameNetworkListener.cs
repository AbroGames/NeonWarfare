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
		Instance.ClearLoadingScreen();
	}
	
	[EventListener(ListenerSide.Client)]
	public static void OnChangeWorldPacket(ChangeWorldPacket changeWorldPacket)
	{
		PackedScene newWorldMainScene = changeWorldPacket.WorldType switch
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
