using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.LoadingScreen;

public partial class ClientGame : Node2D
{
	
	private static ClientGame Instance => ClientRoot.Instance.Game;
	
	public override void _Ready()
	{
		SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType.CONNECTING);
		InitNetwork();
	}
}
