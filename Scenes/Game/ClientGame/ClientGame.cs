using Godot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scripts.Content.GameSettings;
using NeonWarfare.Scripts.Utils.GameSettings;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame : Node2D
{
	
	public GameSettings GameSettings { get; set; }
	
	public override void _Ready()
	{
		SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType.CONNECTING);
		InitNetwork();
	}
}
