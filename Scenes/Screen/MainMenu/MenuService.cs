using Godot;

namespace NeonWarfare;

public static class MenuButtonsService
{
    public static void ChangeMenuFromButtonClick(PackedScene menuChangeTo)
    {
        Root.Instance.MainSceneContainer.GetCurrentStoredNode<MainMenuMainScene>().ChangeMenu(menuChangeTo);
    }
    public static void ShutDown()
    {
        Root.Instance.GetTree().Quit();
    }
}