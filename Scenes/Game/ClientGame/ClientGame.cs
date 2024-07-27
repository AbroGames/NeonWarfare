using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.NetOld.Server;

public partial class ClientGame : Node2D
{
	
	private static ClientGame Instance => ClientRoot.Instance.Game;
	
	public override void _Ready()
	{
		SetDefaultLoadingScreen();
		InitNetwork();
	}
}
