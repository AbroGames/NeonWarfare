using System;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using NeonWarfare.Net;
using NeonWarfare.NetOld.Client;

namespace NeonWarfare;

public partial class ConnectToServerButton : Button
{
    [Export] [NotNull] public LineEdit IpLineEdit { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += OnClick;
    }

    private void OnClick()
    {
        int port = NetworkService.DefaultPort;
        string host = IpLineEdit.Text;
        int pos = host.Find(":");
        if (pos != -1)
        {
            try
            {
                port = host.Substring(pos + 1).ToInt();
                host = host.Remove(pos);
            }
            catch (FormatException e)
            {
                Log.Error(e);
            }
        }

        if (port is <= 0 or > 65535)
        {
            return;
        }

        if (host.Equals(""))
        {
            host = NetworkService.DefaultHost;
        }
        
        ClientGame clientGame = new ClientGame();
        ClientRoot.Instance.SetMainScene(clientGame);
        clientGame.ConnectToServer(host, port);
    }
}