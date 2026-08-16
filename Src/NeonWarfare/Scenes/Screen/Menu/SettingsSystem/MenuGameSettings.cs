using Godot;
using NeonWarfare.Scripts.Service.Settings;

namespace NeonWarfare.Scenes.Screen.Menu.SettingsSystem;

public partial class MenuGameSettings
{
    [Category("Player")]
    [Name("SETTING_MENU__NICK")]
    [Hint("SETTING_MENU__NICK_HINT")]
    public string PlayerName { get; set; } = GameSettings.GetDefault().PlayerNick;

    [Category("Player")]
    [Name("SETTING_MENU__COLOR")]
    [Hint("SETTING_MENU__COLOR_HINT")]
    public Color PlayerColor { get; set; } = GameSettings.GetDefault().PlayerColor;

    [Category("Player")]
    [Name("SETTING_MENU__PLAYER_UID")]
    [Hint("SETTING_MENU__PLAYER_UID_HINT")]
    public string PlayerUid { get; set; } = GameSettings.GetDefault().PlayerUid;

    [Category("Player")]
    [Name("SETTING_MENU__AUTOSAVE")]
    [Hint("SETTING_MENU__AUTOSAVE_HINT")]
    public bool AutoSaveEnabled { get; set; } = GameSettings.GetDefault().AutoSaveEnabled;

    [Category("Player")]
    [Hide]
    public bool PlayerSettingsAcknowledged { get; set; } = GameSettings.GetDefault().PlayerSettingsAcknowledged;

    [Category("Audio")]
    [Name("SETTING_MENU__MASTER_VOLUME")]
    [Hint("SETTING_MENU__MASTER_VOLUME_HINT")]
    [Range(0, 100)]
    public int MasterVolume { get; set; } = GameSettings.GetDefault().MasterVolume;

    [Category("Audio")]
    [Name("SETTING_MENU__SOUNDS_VOLUME")]
    [Hint("SETTING_MENU__SOUNDS_VOLUME_HINT")]
    [Range(0, 100)]
    public int SoundsVolume { get; set; } = GameSettings.GetDefault().SoundsVolume;

    [Category("Audio")]
    [Name("SETTING_MENU__MUSIC_VOLUME")]
    [Hint("SETTING_MENU__MUSIC_VOLUME_HINT")]
    [Range(0, 100)]
    public int MusicVolume { get; set; } = GameSettings.GetDefault().MusicVolume;

    [Category("Graphics")]
    [Name("SETTING_MENU__FULLSCREEN")]
    [Hint("SETTING_MENU__FULLSCREEN_HINT")]
    public bool Fullscreen { get; set; } = GameSettings.GetDefault().Fullscreen;

    [Category("Graphics")]
    [Name("SETTING_MENU__RESOLUTION")]
    [Hint("SETTING_MENU__RESOLUTION_HINT")]
    [Options("1280x720", "1366x768", "1600x900", "1920x1080", "2560x1440", "3840x2160")]
    public string Resolution { get; set; } = GameSettings.GetDefault().Resolution;

    [Category("Interface")]
    [Name("SETTING_MENU__INTERFACE_SIZE")]
    [Hint("SETTING_MENU__INTERFACE_SIZE_HINT")]
    [Options("Small", "Medium", "Large")]
    public string InterfaceSize { get; set; } = GameSettings.GetDefault().InterfaceSize;

    public MenuGameSettings() {}

    public MenuGameSettings(
        string playerName, Color playerColor, string playerUid, bool autoSaveEnabled,
        bool playerSettingsAcknowledged, int masterVolume, int soundsVolume, int musicVolume,
        bool fullscreen, string resolution, string interfaceSize)
    {
        PlayerName = playerName;
        PlayerColor = playerColor;
        PlayerUid = playerUid;
        AutoSaveEnabled = autoSaveEnabled;
        PlayerSettingsAcknowledged = playerSettingsAcknowledged;
        MasterVolume = masterVolume;
        SoundsVolume = soundsVolume;
        MusicVolume = musicVolume;
        Fullscreen = fullscreen;
        Resolution = resolution;
        InterfaceSize = interfaceSize;
    }

    public void Validate()
    {
        PlayerName ??= GameSettings.GetDefault().PlayerNick;
        Resolution ??= GameSettings.GetDefault().Resolution;
        InterfaceSize ??= GameSettings.GetDefault().InterfaceSize;
    }
}
