using Godot;

namespace NeonWarfare.Scripts.GlobalServices.Settings;

public record GameSettings(
    string PlayerNick,
    Color PlayerColor,
    string Locale,
    int AutoSaveIntervalMinutes,
    bool PlayerSettingsAcknowledged,
    int MasterVolume,
    int SoundsVolume,
    int MusicVolume,
    bool Fullscreen,
    string Resolution,
    string InterfaceSize
)
{
    public static GameSettings GetDefault()
    {
        return new(
            PlayerNick: "Player",
            PlayerColor: new Color(1, 1, 1),
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            AutoSaveIntervalMinutes: 10,
            PlayerSettingsAcknowledged: false,
            MasterVolume: 100,
            SoundsVolume: 100,
            MusicVolume: 100,
            Fullscreen: false,
            Resolution: "1280x720",
            InterfaceSize: "Medium"
        );
    }
}