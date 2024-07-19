using Godot;

namespace NeonWarfare;

public static class MenuButtonsService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        ClientRoot.Instance.MainMenu.ChangeMenu(menuChangeTo);
    }
}