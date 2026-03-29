using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.UI.Menu.SettingsSystem;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu.Pages.Main;

public partial class MainPage : MainMenuPage
{
	[Child] public Button ResumeButton { get; private set; }
	[Child] public Button StartSingleplayerButton { get; private set; }
	[Child] public Button CreateServerButton { get; private set; }
	[Child] public Button ConnectToServerButton { get; private set; }
	[Child] public Button SettingsButton { get; private set; }
	[Child] public Button QuitButton { get; private set; }

	private Action _resumeAction = null;

	public override void _EnterTree()
	{
		Callable.From(ConfigureResumeButton).CallDeferred();
	}

	public override void _Ready()
	{
		Di.Process(this);
		ResumeButton.Pressed += RunResumeAction;
		StartSingleplayerButton.Pressed += () =>
		{
			Services.GameSettings.PreserveSingleplayerGame(null);
			Services.MainScene.StartSingleplayerGame();
		};
		CreateServerButton.Pressed += () => GoNext(PagesScenes.CreateServer.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
		ConnectToServerButton.Pressed += () => GoNext(PagesScenes.ConnectionPage.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
		SettingsButton.Pressed += () => GoNext(PagesScenes.SettingsPage.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
		QuitButton.Pressed += () => Services.MainScene.Shutdown();

		ConfigureResumeButton();
	}

	private void RunResumeAction()
	{
		_resumeAction?.Invoke();
	}
	
	private void ConfigureResumeButton()
	{
		var availableResume = Services.GameSettings.Settings.FastResumeAvailable;
		if (availableResume is ResumableGame.None)
		{
			ResumeButton.Visible = false;
		}
		else
		{
			ResumeButton.Visible = true;
			if (availableResume is ResumableGame.RunSingleplayer)
			{
				ResumeButton.Text = "Run: Singleplayer";
				_resumeAction = () => Services.MainScene.StartSingleplayerGame();
			}

			if (availableResume is ResumableGame.ConnectToServer)
			{
				var port = Services.GameSettings.Settings.LastConnectedPort;
				var host = Services.GameSettings.Settings.LastConnectedHost;
				ResumeButton.Text = $"Connect to: {host}:{port}";
				_resumeAction = () => Services.MainScene.ConnectToMultiplayerGame(host, port);
			}

			if (availableResume is ResumableGame.CreateServer)
			{
				var save = Services.GameSettings.Settings.LastHostedSaveName;
				var port  = Services.GameSettings.Settings.LastHostedPort;
				var asDedicated = Services.GameSettings.Settings.LastHostedIsDedicated;
				ResumeButton.Text = $"Create{(asDedicated ? " Dedicated" : "")} Server: {save}@{port}";
				_resumeAction = () => Services.MainScene.HostMultiplayerGameAsClient(port, save, asDedicated);
			}
		}
	}
}