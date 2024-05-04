using System;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public partial class CreateServerButton : Button
{
    [Export] [NotNull] public LineEdit PortLineEdit { get; private set; }
    [Export] [NotNull] public CheckBox ShowConsoleCheckBox { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
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
                return;
        
            EventBus.Publish(new CreateServerRequest(port, Root.Instance.PlayerSettings.PlayerName, ShowConsoleCheckBox.ButtonPressed));
            EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, port));
            if (Root.Instance.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
            {
                Log.Error(
                    "OnCreateServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
                return;
            }
            Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen);
        };
    }

}