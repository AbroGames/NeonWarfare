using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class MenuButtonsService
{
    [EventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
        Network.CreateServer(DefaultNetworkSettings.Port);
    }
    
    
    [EventListener]
    public void OnConnectToServerButtonClickEvent(ConnectToServerButtonClickEvent connectToServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(connectToServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
        Network.ConnectToRemoteServer(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port);
    }
    
    [EventListener]
    public void SettingsButtonClickEvent(SettingsButtonClickEvent settingsButtonClickEvent)
    {
        
    }
    
    [EventListener]
    public void ShutDownEvent(ShutDownEvent shutDownEvent)
    {
        Root.Instance.GetTree().Quit();
    }
}