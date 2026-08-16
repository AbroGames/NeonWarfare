using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.Settings;
using NeonWarfare.Scenes.Screen.Menu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.Menu.MainMenu.Pages.PlayerSettings;

public partial class PlayerSettingsPage : MainMenuPage
{
    [Child] public VBoxContainer SettingsContainer { get; private set; }
    [Child] public Button SaveButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    private MenuGameSettings _preservedSettings;
    private MenuGameSettings _draftSettings;
    private IReadOnlyList<Setting> _settings;
    private Action _continuation;

    public override void _Ready()
    {
        Di.Process(this);

        // Draft/preserved pattern, identical to SettingsPage: both load the live settings;
        // edits mutate Setting.Value (bound to _draftSettings); Save writes back + persists;
        // Cancel re-applies _preservedSettings to roll back any runtime side-effects.
        _preservedSettings = Services.MenuGameSettings.GetSettings();
        _draftSettings = Services.MenuGameSettings.GetSettings();

        SaveButton.Pressed += OnSave;
        CancelButton.Pressed += OnCancel;

        PopulateSettings();
    }

    /// <summary>
    /// Called by <see cref="PagesProvider.PreparePlayerSettingsPage"/>. The action to run after a
    /// successful Save (the real game start). May be null when this page is opened standalone
    /// (e.g. Phase 7 Settings hub reuse without a gate) — in that case Save just returns.
    /// </summary>
    public void SetContinuation(Action continuation) => _continuation = continuation;

    private void PopulateSettings()
    {
        // Renders nick + color + uid + autosave (all non-hidden Player-category fields).
        // PlayerSettingsAcknowledged is [Hide] so it is excluded automatically.
        _settings = _draftSettings.GetVisibleSettings("Player");
        foreach (var setting in _settings)
        {
            SettingsContainer.AddChild(new SettingContainer(setting));
        }
    }

    private void OnSave()
    {
        _draftSettings.SetVisibleSettings(_settings);
        _draftSettings.PlayerSettingsAcknowledged = true; // [Hide] field: set on the draft directly.
        Services.MenuGameSettings.ApplyAndSaveSettings(_draftSettings);

        var continuation = _continuation;
        _continuation = null;
        if (continuation is not null)
        {
            // The continuation starts the game, which swaps the scene and frees this menu.
            continuation.Invoke();
        }
        else
        {
            // Opened standalone (no gate) — just return.
            GoBack();
        }
    }

    private void OnCancel()
    {
        // Roll back any runtime side-effects of in-page edits, then return without starting.
        Services.MenuGameSettings.ApplyAndSaveSettings(_preservedSettings);
        _continuation = null;
        GoBack();
    }
}
