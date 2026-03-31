using Godot;
using NeonWarfare.Scripts.Service.Settings;

namespace NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

public partial class MenuGameSettings
{
    [Name("SETTING_MENU__NICK")]
    [Hint("SETTING_MENU__NICK_HINT")]
    public string PlayerName { get; set; } = GameSettings.GetDefault().PlayerNick;
    
    [Name("SETTING_MENU__COLOR")]
    [Hint("SETTING_MENU__COLOR_HINT")]
    public Color PlayerColor { get; set; } = GameSettings.GetDefault().PlayerColor;

    public MenuGameSettings() {}

    public MenuGameSettings(string playerName, Color playerColor)
    {
        PlayerName = playerName;
        PlayerColor = playerColor;
    }

    public void Validate()
    {
        PlayerName ??= GameSettings.GetDefault().PlayerNick;
    }
}