using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class ToBattleButton : Button
{
    
    public override void _Ready()
    {
        Pressed += () =>
        {
            EventBus.Publish(new ToBattleButtonClickEvent(this));
        };
    }

}