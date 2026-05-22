using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Connect;

public partial class ConnectPage : MainMenuPage
{
    [Child] public LineEdit HostLineEdit { get; private set; }
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public Button ConnectToServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        HostLineEdit.Text = String.Empty;
        PortSpinBox.Value = Consts.DefaultPort;
        
        ConnectToServerButton.Pressed += ParseAndConnectToServer;
        CancelButton.Pressed += () => GoBack();
    }

    private void ParseAndConnectToServer()
    {
        string host = String.IsNullOrWhiteSpace(HostLineEdit.Text) ? null : HostLineEdit.Text.Trim();
        if (host is null)
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("CONNECT_MENU__HOSTNAME_UNSPECIFIED_ERROR")));
            return;
        }
        
        int port = (int) PortSpinBox.Value;
        Services.MainScene.ConnectToMultiplayerGame(host, port);
    }
}