using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;
using NeonWarfare.NetOld.Server;

public partial class ClientGame
{
	
	[EventListener(ListenerSide.Client)]
	public void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		PingChecker.Start();
		ClearLoadingScreen();
	}
	
	[EventListener(ListenerSide.Client)]
	public void OnChangeWorldPacket(ChangeWorldPacket changeWorldPacket)
	{
		PackedScene newWorldMainScene = changeWorldPacket.WorldType switch
		{
			ChangeWorldPacket.ServerWorldType.Safe => ClientRoot.Instance.PackedScenes.SafeWorldMainScene,
			ChangeWorldPacket.ServerWorldType.Battle => ClientRoot.Instance.PackedScenes.BattleWorldMainScene,
			_ => null
		};

		if (newWorldMainScene == null)
		{
			Log.Error($"Received unknown type of WorldMainScene: {changeWorldPacket.WorldType}");
			return;
		}

		ChangeMainScene(newWorldMainScene.Instantiate<IWorldMainScene>());
	}
	
	[EventListener(ListenerSide.Client)]
	public void OnWaitBattleEndPacket(WaitBattleEndPacket emptyPacket)
	{
		SetLoadingScreen(LoadingScreen.Create("Wait for the end of the battle"));
	}
}
