using AbroDraft.Scripts.EventBus;
using Godot;
using KludgeBox;

namespace AbroDraft.Scenes.Screen.Menu.CreateServerButton;

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