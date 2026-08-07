using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Settings;
using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.SettingsCategory;

public partial class SettingsCategoryPage : MainMenuPage
{
    [Child] public Label TitleLabel { get; private set; }
    [Child] public VBoxContainer SettingsContainer { get; private set; }
    [Child] public Button SaveButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    private string _category;
    private string _titleKey;
    private MenuGameSettings _preservedSettings;
    private MenuGameSettings _draftSettings;
    private IReadOnlyList<Setting> _settings;

    public override void _Ready()
    {
        Di.Process(this);

        TitleLabel.Text = Tr(_titleKey);
        _preservedSettings = Services.MenuGameSettings.GetSettings();
        _draftSettings = Services.MenuGameSettings.GetSettings();

        SaveButton.Pressed += OnSave;
        CancelButton.Pressed += OnCancel;
        BackButton.Pressed += OnBack;

        PopulateSettings();
    }

    /// <summary>Called by <see cref="PagesProvider.PrepareSettingsCategoryPage"/> before the page is added to the tree.</summary>
    public void Configure(string category, string titleKey)
    {
        _category = category;
        _titleKey = titleKey;
    }

    private void PopulateSettings()
    {
        // Empty for "Controls" today — that's fine; the scroll area is just empty.
        _settings = _draftSettings.GetVisibleSettings(_category);
        foreach (var setting in _settings)
        {
            SettingsContainer.AddChild(new SettingContainer(setting));
        }
    }

    private bool IsDirty() => _draftSettings.Serialize() != _preservedSettings.Serialize();

    private void OnSave()
    {
        _draftSettings.SetVisibleSettings(_settings);
        Services.MenuGameSettings.ApplyAndSaveSettings(_draftSettings);
        // After a successful save the draft == persisted; refresh the preserved snapshot so a
        // subsequent Back (with no further edits) is clean and does not re-prompt.
        _preservedSettings = Services.MenuGameSettings.GetSettings();
        GoBack();
    }

    private void OnCancel()
    {
        // Discard: re-apply the preserved snapshot to undo any runtime side-effects of edits,
        // then return.
        Services.MenuGameSettings.ApplyAndSaveSettings(_preservedSettings);
        GoBack();
    }

    private void OnBack()
    {
        if (IsDirty())
        {
            GoNext(PagesProvider.PrepareConfirmDialogPage(
                message: Tr("CONFIRM_DIALOG__UNSAVED_CHANGES"),
                onReset: () =>
                {
                    // "Reset changes" = save the draft (commit), then leave.
                    _draftSettings.SetVisibleSettings(_settings);
                    Services.MenuGameSettings.ApplyAndSaveSettings(_draftSettings);
                    GoBack();
                },
                onContinue: () =>
                {
                    // "Continue" = discard the draft, then leave.
                    Services.MenuGameSettings.ApplyAndSaveSettings(_preservedSettings);
                    GoBack();
                }
                // onBack (stay on category page) is null — see ConfirmDialogPage.
            ));
            return;
        }
        GoBack();
    }
}
