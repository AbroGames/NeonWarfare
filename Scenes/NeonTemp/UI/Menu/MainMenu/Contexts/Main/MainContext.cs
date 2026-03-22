using Godot;
using System;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using Kludgeful.Main.ContextSystem;
using Serilog;

public partial class MainContext : Context
{
	[Logger] private ILogger _logger;
	[Child] public Button StartSingleplayerButton { get; private set; }
	[Child] public Button CreateServerButton { get; private set; }
	[Child] public Button ConnectToServerButton { get; private set; }
	[Child] public Button SettingsButton { get; private set; }

	public override void _Ready()
	{
		Di.Process(this);
		StartSingleplayerButton.Pressed += () => Services.MainScene.StartSingleplayerGame();
		//CreateServerButton.Pressed += () => ChangeMenuPage(PackedScenes.CreateServer);
		//ConnectToServerButton.Pressed += () => ChangeMenuPage(PackedScenes.ConnectToServer);
		//SettingsButton.Pressed += () => ChangeMenuPage(PackedScenes.Settings);
		
		//CreateServerButton.Pressed += () => GoNext(_mainMenu.ContextScenesStorage.ConnectionContext.Instantiate<IContext>());
		//ConnectToServerButton.Pressed += () => GoNext(_mainMenu.ContextScenesStorage.ConnectionContext.Instantiate<IContext>());
		//SettingsButton.Pressed += () => GoNext(_mainMenu.ContextScenesStorage.SettingsContext.Instantiate<IContext>());
	}
}
