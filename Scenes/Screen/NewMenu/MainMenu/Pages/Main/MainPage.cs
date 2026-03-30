using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Main;

public partial class MainPage : MainMenuPage
{
	[Child] public Button ResumeButton { get; private set; }
	[Child] public Button StartSingleplayerButton { get; private set; }
	[Child] public Button CreateServerButton { get; private set; }
	[Child] public Button ConnectToServerButton { get; private set; }
	[Child] public Button SettingsButton { get; private set; }
	[Child] public Button LanguageButton { get; private set; }
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
		StartSingleplayerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SingleplayerPage));
		CreateServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.CreateServerPageScene));
		ConnectToServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ConnectionPageScene));
		SettingsButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SettingsPageScene));
		LanguageButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.LanguageSelectionPageScene));
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
				ResumeButton.Text = $"{Tr("MAIN_MENU__RESUME_BUTTON__SINGLEPLAYER")}: {Services.GameSettings.Settings.LastSingleplayerSaveName}";
				_resumeAction = () => Services.MainScene.StartSingleplayerGame();
			}

			if (availableResume is ResumableGame.ConnectToServer)
			{
				var port = Services.GameSettings.Settings.LastConnectedPort;
				var host = Services.GameSettings.Settings.LastConnectedHost;
				ResumeButton.Text = $"{Tr("MAIN_MENU__RESUME_BUTTON__CONNECT")}: {host}:{port}";
				_resumeAction = () => Services.MainScene.ConnectToMultiplayerGame(host, port);
			}

			if (availableResume is ResumableGame.CreateServer)
			{
				var save = Services.GameSettings.Settings.LastHostedSaveName;
				var port  = Services.GameSettings.Settings.LastHostedPort;
				var asDedicated = Services.GameSettings.Settings.LastHostedIsDedicated;
				ResumeButton.Text = $"{Tr("MAIN_MENU__RESUME_BUTTON__HOST")}: {save}@{port}";
				_resumeAction = () => Services.MainScene.HostMultiplayerGameAsClient(port, save, asDedicated);
			}
		}
	}
}