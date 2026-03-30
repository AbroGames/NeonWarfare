using NeonWarfare.Scenes.Screen.NewMenu.PagesSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class MainMenuPage : Page
{
    protected PagesProvider PagesProvider;
    public void SetPagesProvider(PagesProvider availablePages)
    {
        PagesProvider = availablePages;
    }
}

public static class MainMenuPageExtensions
{
    public static TMainMenuPage WithAvailablePages<TMainMenuPage>(this TMainMenuPage page, PagesProvider pagesProvider) where TMainMenuPage : MainMenuPage
    {
        page.SetPagesProvider(pagesProvider);
        return page;
    }
}