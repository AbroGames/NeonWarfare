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
	public void OnChangeWorldPacket(SC_ChangeWorldPacket changeWorldPacket)
	{
		if (!SC_ChangeWorldPacket.WorldScenesMap.TryGetValue(changeWorldPacket.WorldType, out var newWorldMainScene))
		{
			Log.Error($"Received unknown type of WorldMainScene: {changeWorldPacket.WorldType}");
			return;
		}
		
		ChangeMainScene(newWorldMainScene.Instantiate<IWorldMainScene>());
	}
	
	[EventListener(ListenerSide.Client)]
	public void OnWaitBattleEndPacket(SC_WaitBattleEndPacket emptyPacket)
	{
		SetLoadingScreen(LoadingScreen.Create("Wait for the end of the battle"));
	}
}
