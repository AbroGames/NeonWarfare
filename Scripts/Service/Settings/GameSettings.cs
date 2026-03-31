using Godot;

namespace NeonWarfare.Scripts.Service.Settings;

public record GameSettings(
    string PlayerNick,
    Color PlayerColor,
    string Locale
)
{
    public static GameSettings GetDefault()
    {
        return new (
            PlayerNick: "Player",
            PlayerColor: new Color(1, 1, 1),
            Locale: Services.I18N.GetUserOsLocaleInfoOrDefault().Code
        );
    }
    
    public enum ResumableGame
    {
        None,
        RunSingleplayer,
        CreateServer,
        ConnectToServer,
    }
}
    
//[Hide] public ResumableGame FastResumeAvailable { get; set; } = ResumableGame.None;
//    
//[Hide] public string LastSingleplayerSaveName { get; set; }
//[Hide] public string LastConnectedHost { get; set; } = String.Empty;
//[Hide] public int LastHostedPort { get; set; } = Consts.DefaultPort;
//[Hide] public bool LastHostedIsDedicated { get; set; }
//[Hide] public string GameLocale { get; set; } = Services.I18N.GetUserOsLocaleInfoOrDefault().Code;