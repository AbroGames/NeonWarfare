using Godot;
using KludgeBox;
using KludgeBox.Events.Global;

namespace NeoVector;

public partial class ExitButton : Button
{
    public override void _Ready()
    {
        Pressed += MenuButtonsService.ShutDown;
    }
}