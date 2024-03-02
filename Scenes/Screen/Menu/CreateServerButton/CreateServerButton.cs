using Godot;
using KludgeBox;

namespace KludgeBox.Events.Global;

public partial class CreateServerButton : Button
{
    [Export] [NotNull] public PackedScene NewWorldMainScene { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        EventBus.Publish(new CreateServerButtonReadyEvent(this));
        Pressed += () =>
        {
            EventBus.Publish(new CreateServerButtonClickEvent(this));
        };
    }

}