using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class SettingsButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuButtonsService.ChangeMenuFromButtonClick(Root.Instance.PackedScenes.Screen.SettingsMenu);
        };
    }
}