using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public record GameSettings(
    string PlayerUid,
    string PlayerNick,
    Color PlayerColor,
    string Locale,
    ResumableGame LastGame
)
{
    public static GameSettings GetDefault()
    {
        return new(
            PlayerUid: new UidGenerator().Generate(),
            PlayerNick: "Player",
            PlayerColor: new Color(1, 1, 1),
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            LastGame: ResumableGame.GetNone()
        );
    }
}