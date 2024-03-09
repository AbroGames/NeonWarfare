using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class SettingsButton : Button
{
    public override void _Ready()
    {
        Pressed += () =>
        {
            EventBus.Publish(new SettingsButtonClickEvent(this));
        };
    }
}