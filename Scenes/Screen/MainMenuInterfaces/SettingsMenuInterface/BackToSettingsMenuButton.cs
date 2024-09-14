using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class BackToSettingsMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.SettingsMenu);
        };
    }
}