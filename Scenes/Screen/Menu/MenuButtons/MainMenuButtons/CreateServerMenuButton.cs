using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class CreateServerMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            EventBus.Publish(new ChangeMenuFromButtonClickRequest(Root.Instance.PackedScenes.Screen.CreateServerMenu));
        };
    }
}