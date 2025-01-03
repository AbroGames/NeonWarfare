using Godot;
using NeonWarfare.Scenes.Screen.LoadingScreen;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame : Node2D
{
	
	public override void _Ready()
	{
		SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType.CONNECTING);
		InitNetwork();
	}
}
