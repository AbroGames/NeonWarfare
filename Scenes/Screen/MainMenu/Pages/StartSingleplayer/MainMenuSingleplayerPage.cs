using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.StartSingleplayer;

public partial class MainMenuSingleplayerPage : MainMenuPage
{
    
    [Child] public TextEdit SaveNameTextEdit { get; private set; }
    [Child] public Button StartGameButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        StartGameButton.Pressed += ParseAndStartServer;
    }

    private void ParseAndStartServer()
    {
        string saveFileName = SaveNameTextEdit.Text.Length != 0 ? SaveNameTextEdit.Text : null;
        Services.MainScene.StartSingleplayerGame(saveFileName);
    }
}