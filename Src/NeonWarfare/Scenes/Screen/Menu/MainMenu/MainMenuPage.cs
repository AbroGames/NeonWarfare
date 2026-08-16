using System;
using NeonWarfare.Scenes.Screen.Menu.PagesSystem;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu;

public partial class MainMenuPage : Page
{
    protected PagesProvider PagesProvider;
    public void SetPagesProvider(PagesProvider availablePages)
    {
        PagesProvider = availablePages;
    }

    /// <summary>
    /// First-run gate (#16). Runs <paramref name="startAction"/> immediately when the player
    /// has already acknowledged their settings; otherwise pushes <see cref="NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.PlayerSettings.PlayerSettingsPage"/>
    /// with <paramref name="startAction"/> as the post-save continuation.
    /// </summary>
    protected void TryStartGame(Action startAction)
    {
        if (Services.GameSettings.GetSettings().PlayerSettingsAcknowledged)
        {
            startAction();
            return;
        }
        GoNext(PagesProvider.PreparePlayerSettingsPage(startAction));
    }
}

public static class MainMenuPageExtensions
{
    public static TMainMenuPage WithAvailablePages<TMainMenuPage>(
        this TMainMenuPage page, PagesProvider pagesProvider) where TMainMenuPage : MainMenuPage
    {
        page.SetPagesProvider(pagesProvider);
        return page;
    }
}