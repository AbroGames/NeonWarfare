using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare;

public partial class ClientGame : Node2D
{
	
	private static ClientGame Instance => ClientRoot.Instance.Game;
	
	public override void _Ready()
	{
		SetDefaultLoadingScreen();
		InitNetwork();
	}
}
