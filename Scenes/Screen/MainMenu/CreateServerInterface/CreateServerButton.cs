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
        
        NetworkService.CreateServer(port, ClientRoot.Instance.PlayerSettings.PlayerName, ShowConsoleCheckBox.ButtonPressed);
        NetworkService.ConnectToServer(NetworkService.DefaultHost, port);
            
        if (Root.Instance.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error("OnCreateServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen);
    }

}