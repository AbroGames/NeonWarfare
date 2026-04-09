using NeonWarfare.Scenes.NeonTemp.UI.Menu.PagesSystem;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu;

public partial class MainMenuPage : Page
{
    protected PagesScenes PagesScenes;
    public MainMenuPage WithAvailablePages(PagesScenes availablePages)
    {
        PagesScenes = availablePages;
        return this;
    }
}