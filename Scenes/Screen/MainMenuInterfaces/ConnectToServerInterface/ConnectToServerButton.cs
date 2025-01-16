using System;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.ConnectToServerInterface;

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
        int port = Network.DefaultPort;
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
            host = Network.DefaultHost;
        }
        
        ClientRoot.Instance.CreateClientGame(host, port);
    }
}
