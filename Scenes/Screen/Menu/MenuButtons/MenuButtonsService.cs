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
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
        EventBus.Publish(new CreateServerRequest(DefaultNetworkSettings.Port, "Player"));
        EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port));
    }
    
    
    [EventListener]
    public void OnConnectToServerButtonClickEvent(ConnectToServerButtonClickEvent connectToServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(connectToServerButtonClickEvent.ConnectToServerButton.NewWorldMainScene.Instantiate());
        EventBus.Publish(new ConnectToServerRequest(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port));
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