using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.CommonInterface;

public partial class BackToMainMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.MainMenu);
        };
    }
}
