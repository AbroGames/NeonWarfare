using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu.Pages.Main;

public partial class MainPage : MainMenuPage
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
		CreateServerButton.Pressed += () => GoNext(PagesScenes.CreateServer.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
		ConnectToServerButton.Pressed += () => GoNext(PagesScenes.ConnectionPage.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
		SettingsButton.Pressed += () => GoNext(PagesScenes.SettingsPage.Instantiate<MainMenuPage>().WithAvailablePages(PagesScenes));
	}
}