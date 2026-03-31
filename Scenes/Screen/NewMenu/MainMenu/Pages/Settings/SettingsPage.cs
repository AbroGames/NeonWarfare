using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Settings;

public partial class SettingsPage : MainMenuPage
{
    [Child] public Button SaveButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public VBoxContainer SettingsContainer { get; private set; }
    
    private MenuGameSettings _preservedSettings;
    private MenuGameSettings _draftSettings;
    
    private IReadOnlyList<Setting> _settings;

    public override void _Ready()
    {
        Di.Process(this);

        _preservedSettings = Services.MenuGameSettings.GetSettings();
        _draftSettings = Services.MenuGameSettings.GetSettings();
        
        SaveButton.Pressed += OnSave;
        CancelButton.Pressed += OnCancel;
        
        PopulateSettings();
    }

    private void PopulateSettings()
    {
        _settings = _draftSettings.GetVisibleSettings();
        foreach (var setting in _settings)
        {
            var settingContainer = new SettingContainer(setting);
            SettingsContainer.AddChild(settingContainer);
        }
    }

    private void OnSave()
    {
        _draftSettings.SetVisibleSettings(_settings);
        Services.MenuGameSettings.ApplyAndSaveSettings(_draftSettings);
        GoBack();
    }

    private void OnCancel()
    {
        Services.MenuGameSettings.ApplyAndSaveSettings(_preservedSettings);
        GoBack();
    }
}