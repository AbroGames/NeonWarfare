using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class CreateServerMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuButtonsService.ChangeMenuFromButtonClick(Root.Instance.PackedScenes.Screen.CreateServerMenu);
        };
    }
}