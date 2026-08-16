using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.SettingsCategory;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.SettingsHub;

public partial class SettingsHubPage : MainMenuPage
{
    [Child] public Button PlayerButton { get; private set; }
    [Child] public Button ControlsButton { get; private set; }
    [Child] public Button InterfaceButton { get; private set; }
    [Child] public Button GraphicsButton { get; private set; }
    [Child] public Button AudioButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);

        PlayerButton.Pressed += ()
            => GoNext(PagesProvider.PrepareSettingsCategoryPage("Player", "SETTINGS_HUB__PLAYER_TITLE"));
        ControlsButton.Pressed += ()
            => GoNext(PagesProvider.PrepareSettingsCategoryPage("Controls", "SETTINGS_HUB__CONTROLS_TITLE"));
        InterfaceButton.Pressed += ()
            => GoNext(PagesProvider.PrepareSettingsCategoryPage("Interface", "SETTINGS_HUB__INTERFACE_TITLE"));
        GraphicsButton.Pressed += ()
            => GoNext(PagesProvider.PrepareSettingsCategoryPage("Graphics", "SETTINGS_HUB__GRAPHICS_TITLE"));
        AudioButton.Pressed += ()
            => GoNext(PagesProvider.PrepareSettingsCategoryPage("Audio", "SETTINGS_HUB__AUDIO_TITLE"));
        BackButton.Pressed += GoBack;
    }
}
