using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public record GameSettings(
    string PlayerUid,
    string PlayerNick,
    Color PlayerColor,
    string Locale,
    bool AutoSaveEnabled,
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
            PlayerUid: new UidGenerator().Generate(),
            PlayerNick: "Player",
            PlayerColor: new Color(1, 1, 1),
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            AutoSaveEnabled: true,
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