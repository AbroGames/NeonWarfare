using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.NeonTemp.UI.Menu.SettingsSystem;

namespace NeonWarfare.Scenes.NeonTemp.UI.Menu.MainMenu.Pages.Settings;

public partial class SettingsPage : MainMenuPage
{
    [Child] public Button SaveButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public VBoxContainer SettingsContainer { get; private set; }
    
    private IReadOnlyList<Setting> _settings;

    public override void _Ready()
    {
        Di.Process(this);
        
        SaveButton.Pressed += OnSave;
        CancelButton.Pressed += OnCancel;
        
        PopulateSettings();
    }

    private void PopulateSettings()
    {
        _settings = Services.GameSettings.Settings.GetVisibleSettings();
        foreach (var setting in _settings)
        {
            var settingContainer = new SettingContainer(setting);
            SettingsContainer.AddChild(settingContainer);
        }
    }

    private void OnSave()
    {
        Services.GameSettings.Settings.SetVisibleSettings(_settings);
        Services.GameSettings.SaveSettings();
        GoBack();
    }

    private void OnCancel()
    {
        GoBack();
    }
}