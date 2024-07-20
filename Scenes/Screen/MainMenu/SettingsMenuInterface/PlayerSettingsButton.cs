using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class PlayerSettingsButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuButtonsService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.Screen.PlayerSettingsMenu);
        };
    }
}