using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class GraphicSettingsButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuButtonsService.ChangeMenuFromButtonClick(Root.Instance.PackedScenes.Screen.GraphicSettingsMenu);
        };
    }
}