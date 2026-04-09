using NeonWarfare.Scenes.Screen.NewMenu.PagesSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class MainMenuPage : Page
{
    protected PagesScenes PagesScenes;
    public MainMenuPage WithAvailablePages(PagesScenes availablePages)
    {
        PagesScenes = availablePages;
        return this;
    }
}