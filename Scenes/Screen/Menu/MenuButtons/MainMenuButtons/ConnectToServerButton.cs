using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class ConnectToServerButton : Button
{
    [Export] [NotNull] public PackedScene NewWorldMainScene { get; private set; }
	
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            EventBus.Publish(new ConnectToServerButtonClickEvent(this));
        };
    }
}