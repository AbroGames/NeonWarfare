using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public record GameSettings(
    string PlayerNick,
    Color PlayerColor,
    string Locale,
    GameSettings.ResumableGame LastGame
)
{
    public static GameSettings GetDefault()
    {
        return new(
            PlayerNick: "Player",
            PlayerColor: new Color(1, 1, 1),
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code,
            LastGame: new ResumableGame(
                Type: ResumableGame.ResumableType.None,
                SaveName: null,
                Host: null,
                Port: null,
                IsDedicated: false)
        );
    }

    public record ResumableGame(
        ResumableGame.ResumableType Type,
        string SaveName,
        string Host,
        string Port,
        bool IsDedicated
    )
    {
        public enum ResumableType
        {
            None,
            RunSingleplayer,
            CreateServer,
            ConnectToServer,
        }
    }
}