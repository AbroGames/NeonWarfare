using Godot;

namespace NeonWarfare;

public static class MenuService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        ClientRoot.Instance.MainMenu.ChangeMenu(menuChangeTo);
    }
}