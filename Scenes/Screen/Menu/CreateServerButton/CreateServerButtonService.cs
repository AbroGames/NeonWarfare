using KludgeBox;
using KludgeBox.Events;
using AbroDraft.Scenes.Root;

namespace AbroDraft.Scenes.Screen.Menu.CreateServerButton;

[GameService]
public class CreateServerButtonService
{
    [GameEventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        Root.Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
    }
}