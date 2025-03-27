using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scripts.Content.GameSettings;
using NeonWarfare.Scripts.Utils.GameSettings;
using NeonWarfare.Scripts.Utils.Profiling;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame : Node2D
{
	public GameSettings GameSettings { get; set; }
	public Dictionary<string, string> AchievementTrackers { get; private set; } = new ();
	
	public override void _Ready()
	{
		ClientRoot.Instance.SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType.CONNECTING);
		InitNetwork();
	}
}
