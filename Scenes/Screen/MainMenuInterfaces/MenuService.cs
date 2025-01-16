using Godot;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces;

public static class MenuService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        ClientRoot.Instance.MainMenu.ChangeMenu(menuChangeTo);
    }
}
