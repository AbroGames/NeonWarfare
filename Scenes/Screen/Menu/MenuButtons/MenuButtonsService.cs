using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class MenuButtonsService
{
    [EventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        int port = 0;
        try
        {
            port = createServerButtonClickEvent.CreateServerButton.PortLineEdit.Text.ToInt();
        }
        catch (FormatException e)
        {
            Log.Error(e);
        }

        if (port <= 0 || port > 65535)
            return;
        
        EventBus.Publish(new CreateServerRequest(port, Root.Instance.Game.PlayerInfo.PlayerName));
        EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, port));
        if (Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error(
                "OnCreateServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().MenuContainer.ChangeStoredNode(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen.Instantiate());
    }
    
    
    [EventListener]
    public void OnConnectToServerButtonClickEvent(ConnectToServerButtonClickEvent connectToServerButtonClickEvent)
    {
        EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port));
        if (Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error(
                "OnConnectToServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().MenuContainer.ChangeStoredNode(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen.Instantiate());
    }
    
    [EventListener]
    public void OnChangeMenuFromButtonClickRequest(ChangeMenuFromButtonClickRequest changeMenuFromButtonClickRequest)
    {
        Root.Instance.Game.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().MenuContainer.ChangeStoredNode(changeMenuFromButtonClickRequest.MenuChangeTo.Instantiate());
    }
    
    [EventListener]
    public void OnShutDownEvent(ShutDownEvent shutDownEvent)
    {
        Root.Instance.GetTree().Quit();
    }
    
    [EventListener]
    public void OnSavePlayerSettingButtonClickEvent(SavePlayerSettingButtonClickEvent savePlayerSettingButtonClickEvent)
    {
        string newNickname = savePlayerSettingButtonClickEvent.SavePlayerSettingsButton.NickLineEdit.Text;
        Color newColor = savePlayerSettingButtonClickEvent.SavePlayerSettingsButton.ColorRect.Color;
        Root.Instance.Game.PlayerInfo.PlayerName = newNickname;
        Root.Instance.Game.PlayerInfo.PlayerColor = newColor;
    }
}