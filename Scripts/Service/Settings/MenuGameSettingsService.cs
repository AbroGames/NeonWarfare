using Godot;
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
        ApplyRuntimeSettings(gameSettings);
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
            playerColor: gameSettings.PlayerColor,
            playerUid: gameSettings.PlayerUid,
            autoSaveEnabled: gameSettings.AutoSaveEnabled,
            playerSettingsAcknowledged: gameSettings.PlayerSettingsAcknowledged,
            masterVolume: gameSettings.MasterVolume,
            soundsVolume: gameSettings.SoundsVolume,
            musicVolume: gameSettings.MusicVolume,
            fullscreen: gameSettings.Fullscreen,
            resolution: gameSettings.Resolution,
            interfaceSize: gameSettings.InterfaceSize
        );
    }

    private GameSettings Convert(MenuGameSettings menuGameSettings)
    {
        return new GameSettings(
            PlayerUid: menuGameSettings.PlayerUid,
            PlayerNick: menuGameSettings.PlayerName,
            PlayerColor: menuGameSettings.PlayerColor,
            Locale: Services.GameSettings.GetSettings().Locale,
            AutoSaveEnabled: menuGameSettings.AutoSaveEnabled,
            PlayerSettingsAcknowledged: menuGameSettings.PlayerSettingsAcknowledged,
            MasterVolume: menuGameSettings.MasterVolume,
            SoundsVolume: menuGameSettings.SoundsVolume,
            MusicVolume: menuGameSettings.MusicVolume,
            Fullscreen: menuGameSettings.Fullscreen,
            Resolution: menuGameSettings.Resolution,
            InterfaceSize: menuGameSettings.InterfaceSize
        );
    }

    private void ApplyRuntimeSettings(GameSettings gameSettings)
    {
        var mode = gameSettings.Fullscreen
            ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.Windowed;
        DisplayServer.WindowSetMode(mode);

        if (TryParseResolution(gameSettings.Resolution, out int width, out int height))
        {
            DisplayServer.WindowSetSize(new Vector2I(width, height));
        }

        SetBusVolume("Master", gameSettings.MasterVolume);
        SetBusVolume("Sounds", gameSettings.SoundsVolume);
        SetBusVolume("Music", gameSettings.MusicVolume);
    }

    private static bool TryParseResolution(string resolution, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (string.IsNullOrWhiteSpace(resolution)) return false;
        var parts = resolution.Split('x');
        if (parts.Length != 2) return false;
        return int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height) && width > 0 && height > 0;
    }

    private static void SetBusVolume(string busName, int volume0to100)
    {
        int busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex < 0) return;

        if (volume0to100 <= 0)
        {
            AudioServer.SetBusMute(busIndex, true);
            return;
        }

        AudioServer.SetBusMute(busIndex, false);
        AudioServer.SetBusVolumeDb(busIndex, (float)Mathf.LinearToDb(volume0to100 / 100.0));
    }
}
