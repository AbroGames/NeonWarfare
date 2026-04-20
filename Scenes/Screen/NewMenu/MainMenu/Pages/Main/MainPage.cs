using System;
using Godot;
using GodotTemplate.Scripts.Service.ResumableGame;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;
using NeonWarfare.Scripts.Service.Settings;

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

	public override void _EnterTree()
	{
		Callable.From(ConfigureResumeButton).CallDeferred();
	}

	public override void _Ready()
	{
		Di.Process(this);
		ResumeButton.Pressed += () => Services.LastGame.StartLastGame();
		StartSingleplayerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SingleplayerPage));
		CreateServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.CreateServerPageScene));
		ConnectToServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ConnectionPageScene));
		SettingsButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SettingsPageScene));
		LanguageButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.LanguageSelectionPageScene));
		QuitButton.Pressed += () => Services.MainScene.Shutdown();

		ConfigureResumeButton();
	}
	
	private void ConfigureResumeButton()
	{
		var lastGame = Services.LastGame.GetLastGame();
		if (lastGame.Type is ResumableGame.ResumableType.None)
		{
			ResumeButton.Visible = false;
		}
		else
		{
			ResumeButton.Visible = true;
			ResumeButton.Text = lastGame.Type switch
			{
				ResumableGame.ResumableType.RunSingleplayer =>
					$"{Tr("MAIN_MENU__RESUME_BUTTON__SINGLEPLAYER")}: {lastGame.SaveName}",
				ResumableGame.ResumableType.ConnectToServer =>
					$"{Tr("MAIN_MENU__RESUME_BUTTON__CONNECT")}: {lastGame.Host}:{lastGame.Port}",
				ResumableGame.ResumableType.CreateServer =>
					$"{Tr("MAIN_MENU__RESUME_BUTTON__HOST")}: {lastGame.SaveName}@{lastGame.Port}",
				_ => string.Empty
			};
		}
	}
}