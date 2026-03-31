using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

namespace NeonWarfare.Scripts.Service.Settings;

public class MenuGameSettingsService
{
    
    public MenuGameSettings GetSettings()
    {
        return Convert(Services.GameSettings.GetSettings());
    }

    public void ApplySettings(MenuGameSettings menuGameSettings)
    {
        GameSettings gameSettings = Convert(menuGameSettings);
        Services.I18N.SetCurrentLocale(gameSettings.Locale);
    }
    
    public void ApplyAndSaveSettings(MenuGameSettings menuGameSettings)
    {
        ApplySettings(menuGameSettings);
        Services.GameSettings.SetSettings(Convert(menuGameSettings));
    }

    private MenuGameSettings Convert(GameSettings gameSettings)
    {
        return new MenuGameSettings(
            playerName: gameSettings.PlayerNick,
            playerColor: gameSettings.PlayerColor
        );
    }

    private GameSettings Convert(MenuGameSettings menuGameSettings)
    {
        return new GameSettings(
            PlayerNick: menuGameSettings.PlayerName,
            PlayerColor: menuGameSettings.PlayerColor,
            Locale: Services.GameSettings.GetSettings().Locale
        );
    }
}