using System;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Net;
using NeoVector;

namespace NeonWarfare;

public partial class ConnectToServerButton : Button
{
    [Export] [NotNull] public LineEdit IpLineEdit { get; private set; }
    [Export] [NotNull] public LineEdit PortLineEdit { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            int port = 0;
            string host = IpLineEdit.Text;
            try
            {
                port = PortLineEdit.Text.ToInt();
            }
            catch (FormatException e)
            {
                Log.Error(e);
            }

            if (port <= 0 || port > 65535)
                return;

            if (host.Equals(""))
                host = DefaultNetworkSettings.Host;
        
        
            EventBus.Publish(new ConnectToServerRequest(host, port));
            if (Root.Instance.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
            {
                Log.Error(
                    "OnConnectToServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
                return;
            }
            Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen);

        };
    }
}