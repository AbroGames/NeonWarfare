using Godot;
using KludgeBox;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces.MainMenuInterface;

public partial class CreateServerMenuButton : Button
{
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        Pressed += () =>
        {
            MenuService.ChangeMenuFromButtonClick(ClientRoot.Instance.PackedScenes.CreateServerMenu);
        };
    }
}
