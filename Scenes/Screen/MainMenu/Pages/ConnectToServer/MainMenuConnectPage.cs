using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using KludgeBox.DI.Requests.NotNullCheck;

namespace NeonWarfare.Scenes.Screen.MainMenu.Pages.ConnectToServer;

public partial class MainMenuConnectPage : MainMenuPage
{
    
    [Child] public TextEdit HostTextEdit { get; private set; }
    [Child] public TextEdit PortTextEdit { get; private set; }
    [Child] public Button ConnectToServerButton { get; private set; }
    
    public override void _Ready()
    {
        Di.Process(this);

        ConnectToServerButton.Pressed += ParseAndConnectToServer;
    }

    private void ParseAndConnectToServer()
    {
        string host = HostTextEdit.Text.Length != 0 ? HostTextEdit.Text : null;
        int? port = PortTextEdit.Text.Length != 0 ? PortTextEdit.Text.ToInt() : null;
        Services.MainScene.ConnectToMultiplayerGame(host, port);
    }
}