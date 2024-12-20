using System;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Networking;
using NeonWarfare.Net;

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
        
        int serverPid = ProcessesService.StartNewDedicatedServerApplication(port, ClientRoot.Instance.PlayerSettings.PlayerName, ShowConsoleCheckBox.ButtonPressed);
        ClientRoot.Instance.CreateClientGame(Network.DefaultHost, port, serverPid);
    }

}