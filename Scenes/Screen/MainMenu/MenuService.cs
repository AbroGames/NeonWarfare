using Godot;

namespace NeonWarfare;

public static class MenuService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        ClientRoot.Instance.MainMenu.ChangeMenu(menuChangeTo);
    }

    public static void ActivateMainMenu()
    {
        var mainMenu = ClientRoot.Instance.PackedScenes.Client.Screens.MainMenuPackedScene;
        ClientRoot.Instance.SetMainScene(mainMenu.Instantiate<MainMenuMainScene>());
    }
}