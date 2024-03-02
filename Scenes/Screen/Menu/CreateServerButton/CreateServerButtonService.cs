using KludgeBox;
using KludgeBox.Events;

namespace KludgeBox.Events.Global;

[GameService]
public class CreateServerButtonService
{
    [EventListener]
    public void OnCreateServerButtonClickEvent(CreateServerButtonClickEvent createServerButtonClickEvent)
    {
        Root.Instance.Game.MainSceneContainer.ChangeStoredNode(createServerButtonClickEvent.CreateServerButton.NewWorldMainScene.Instantiate());
    }
}