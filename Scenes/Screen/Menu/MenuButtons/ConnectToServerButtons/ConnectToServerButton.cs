using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class ConnectToServerButton : Button
{
    [Export] [NotNull] public LineEdit IpLineEdit { get; private set; }
    [Export] [NotNull] public LineEdit PortLineEdit { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            EventBus.Publish(new ConnectToServerButtonClickEvent(this));
        };
    }
}