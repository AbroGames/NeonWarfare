using KludgeBox;
using KludgeBox.Events;

namespace AbroDraft;

[GameService]
public class CreateServerButtonService
{
    [GameEventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
    }
}