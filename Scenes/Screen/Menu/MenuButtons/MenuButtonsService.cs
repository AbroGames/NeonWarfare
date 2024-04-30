using System;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

//TODO Vaster, распили этот класс. Повыноси логику в ноды или сделать static сервис без Event-records
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
        
        EventBus.Publish(new CreateServerRequest(port, Root.Instance.PlayerSettings.PlayerName, createServerButtonClickEvent.CreateServerButton.ShowConsoleCheckBox.ButtonPressed));
        EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, port));
        if (Root.Instance.MainSceneContainer.GetCurrentStoredNode<Node>() is not MainMenuMainScene)
        {
            Log.Error(
                "OnCreateServerButtonClickEvent, MainSceneContainer contains Node that is not MainMenuMainScene");
            return;
        }
        Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(Root.Instance.PackedScenes.Screen.WaitingConnectionScreen);
    }
    
    
    [EventListener]
    public void OnConnectToServerButtonClickEvent(ConnectToServerButtonClickEvent connectToServerButtonClickEvent)
    {
        int port = 0;
        string host = connectToServerButtonClickEvent.ConnectToServerButton.IpLineEdit.Text;
        try
        {
            port = connectToServerButtonClickEvent.ConnectToServerButton.PortLineEdit.Text.ToInt();
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
    }
    
    [EventListener]
    public void OnChangeMenuFromButtonClickRequest(ChangeMenuFromButtonClickRequest changeMenuFromButtonClickRequest)
    {
        Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(changeMenuFromButtonClickRequest.MenuChangeTo);
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
        Root.Instance.PlayerSettings.PlayerName = newNickname;
        Root.Instance.PlayerSettings.PlayerColor = newColor;
        SettingsService.PlayerSettingsSave();
    }
}