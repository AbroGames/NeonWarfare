using KludgeBox;
using KludgeBox.Events;

namespace NeoVector;

[GameService]
public class CreateServerButtonService
{
    [EventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
    }
}