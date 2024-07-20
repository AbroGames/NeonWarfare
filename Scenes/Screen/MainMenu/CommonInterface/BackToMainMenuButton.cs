using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BackToMainMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuButtonsService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.Screen.MainMenu);
        };
    }
}