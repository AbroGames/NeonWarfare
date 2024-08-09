using System;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using NeonWarfare.Net;
using NeonWarfare.NetOld.Client;

namespace NeonWarfare;

public partial class CreateServerButton : Button
{
    [Export] [NotNull] public LineEdit PortLineEdit { get; private set; }
    [Export] [NotNull] public CheckBox ShowConsoleCheckBox { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += OnClick;
    }

    private void OnClick()
    {
        int port = 0;
        try
        {
            port = PortLineEdit.Text.ToInt();
        }
        catch (FormatException e)
        {
            Log.Error(e);
        }

        if (port <= 0 || port > 65535)
        {
            return;
        }
        
        NetworkService.StartNewDedicatedServerApplication(port, ClientRoot.Instance.PlayerSettings.PlayerName, ShowConsoleCheckBox.ButtonPressed);

        ClientGame clientGame = new ClientGame();
        ClientRoot.Instance.SetMainScene(clientGame);
        clientGame.ConnectToServer(NetworkService.DefaultHost, port);
    }

}