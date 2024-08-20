using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld;

public partial class ClientGame : Node2D
{
	
	public override void _Ready()
	{
		SetDefaultLoadingScreen();
		InitNetwork();
	}
	
	[EventListener]
	public static void OnConnectedToServerEvent(ConnectedToServerEvent connectedToServerEvent)
	{
		ClientRoot.Instance.GetWindow().MoveToForeground();
		ClientRoot.Instance.Game.ClearLoadingScreen(); //TODO в идеале вызывать только после синхронизации всех стартовых объектов (сервер должен отправить специальный пакет о том, что синхронизация закончена)
	}
}
