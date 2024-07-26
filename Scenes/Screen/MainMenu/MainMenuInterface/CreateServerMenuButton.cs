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
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.Screen.CreateServerMenu);
        };
    }
}