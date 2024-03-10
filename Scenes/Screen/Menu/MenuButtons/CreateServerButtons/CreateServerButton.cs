using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class CreateServerButton : Button
{
    [Export] [NotNull] public LineEdit PortLineEdit { get; private set; }
    [Export] [NotNull] public CheckBox ShowConsoleCheckBox { get; private set; }
	
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