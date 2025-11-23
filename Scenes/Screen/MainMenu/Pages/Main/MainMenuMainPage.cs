using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.Main;

public partial class MainMenuMainPage : MainMenuPage
{
    
    [Child] public Button StartSingleplayerButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button ConnectToServerButton { get; private set; }
    [Child] public Button SettingsButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);
        
        StartSingleplayerButton.Pressed += () => Services.MainScene.StartSingleplayerGame();
        CreateServerButton.Pressed += () => ChangeMenuPage(PackedScenes.CreateServer);
        ConnectToServerButton.Pressed += () => ChangeMenuPage(PackedScenes.ConnectToServer);
        SettingsButton.Pressed += () => ChangeMenuPage(PackedScenes.Settings);
    }
}