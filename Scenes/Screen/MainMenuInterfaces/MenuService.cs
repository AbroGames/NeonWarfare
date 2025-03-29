using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;

namespace NeonWarfare.Scenes.Screen.MainMenuInterfaces;

public static class MenuService
{
    public static Node ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        return ClientRoot.Instance.MainMenu.ChangeMenu(menuChangeTo);
    }
}
