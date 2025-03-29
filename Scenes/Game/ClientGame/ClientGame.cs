using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Screen.LoadingScreen;
using NeonWarfare.Scenes.Screen.MainMenuInterfaces;
using NeonWarfare.Scripts.Content.GameSettings;
using NeonWarfare.Scripts.Utils.GameSettings;
using NeonWarfare.Scripts.Utils.Profiling;

namespace NeonWarfare.Scenes.Game.ClientGame;

public partial class ClientGame : Node2D
{
	public enum GameState
	{
		None,
		Connecting,
		SynchronizingStates,
		WaitingForBattleEnd,
		Playing,
		Disconnecting,
	}
	public GameSettings GameSettings { get; set; }
	public Dictionary<string, string> AchievementTrackers { get; private set; } = new ();
	public GameState CurrentGameState { get; set; } = GameState.None;
	
	public override void _Ready()
	{
		ClientRoot.Instance.SetLoadingScreen(LoadingScreenBuilder.LoadingScreenType.CONNECTING);
		InitNetwork();
	}

	private void ExitToDisconnectedScreen(SC_DisconnectedFromServerPacket screenInfo)
	{
		ClientRoot.Instance.CreateMainMenu();
		ClientRoot.Instance.ClearLoadingScreen();
		Callable.From(
				() =>
				{
					var screen = MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.DisconnectedScreen) as DisconnectedScreen;
					screen.InitializeFrom(screenInfo);
				}
			)
			.CallDeferred();
	}
}
