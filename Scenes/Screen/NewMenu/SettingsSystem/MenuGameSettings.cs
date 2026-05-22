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

    [Name("SETTING_MENU__PLAYER_UID")]
    [Hint("SETTING_MENU__PLAYER_UID_HINT")]
    public string PlayerUid { get; set; } = GameSettings.GetDefault().PlayerUid;
    
    [Name("SETTING_MENU__AUTOSAVE")]
    [Hint("SETTING_MENU__AUTOSAVE_HINT")]
    public bool AutoSaveEnabled { get; set; } = GameSettings.GetDefault().AutoSaveEnabled;

    public MenuGameSettings() {}

    public MenuGameSettings(string playerName, Color playerColor, string playerUid, bool autoSaveEnabled)
    {
        PlayerName = playerName;
        PlayerColor = playerColor;
        PlayerUid = playerUid;
        AutoSaveEnabled = autoSaveEnabled;
    }

    public void Validate()
    {
        PlayerName ??= GameSettings.GetDefault().PlayerNick;
    }
}