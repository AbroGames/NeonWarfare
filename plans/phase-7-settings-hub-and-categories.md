# Phase 7 — Settings hub + 5 category pages + Unsaved-changes dialog (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 7 of 8. **Blocked by:** Phase 1 (settings categorisation + `[Options]` dropdowns + persistence all done), Phase 5 (`PlayerSettingsPage` gate page exists), Phase 6 (`ColorPickerPanel` upgrades the Player color row automatically). **Prerequisite for:** Phase 8 (final locale pass + manual verification). All Phase-7 code compiles against what Phases 1-6 already shipped.

This phase replaces the flat `SettingsPage` (one scrolling list of every visible setting) with a **two-level** structure: `SettingsHubPage` (5 neon category buttons) → one generic `SettingsCategoryPage` per category. It also adds a **`ConfirmDialogPage`** used when the user hits Back on a category page with unsaved edits. The flat `SettingsPage` and its scene are deleted.

The hub looks like `MultiplayerPage` (neon button list). Each category page looks like the old `SettingsPage` (PanelContainer form shell, scroll, Save/Cancel). The confirm dialog looks like `MessagePage` (centered PanelContainer, message + buttons).

## Goal (done = all of)

1. `SettingsHubPage` exists under `Pages/SettingsHub/` (`.cs` + `.tscn`), is a neon-button hub (5 category buttons + Back), and `MainPage.SettingsButton` navigates to it. Hub buttons GoNext to a `SettingsCategoryPage` configured for the clicked category.
2. A single generic `SettingsCategoryPage` exists under `Pages/SettingsCategory/` (`.cs` + `.tscn`). It takes a category name + title key, renders that category's visible settings through the existing `SettingContainer` pipeline, has Save / Cancel, and on Back with dirty state pushes `ConfirmDialogPage` (Reset changes / Back / Continue). Five category instances are reachable from the hub: Player, Controls, Interface, Graphics, Audio.
3. `Controls` category renders empty (no settings carry `[Category("Controls")]` yet) — the page must still work (empty scroll area, Back works, Save is a no-op clean GoBack).
4. `Player` category in the hub is the **generic** `SettingsCategoryPage("Player")` — a *new* path, separate from the Phase-5 `PlayerSettingsPage` gate. The two coexist: gate uses `PlayerSettingsPage` (continuation + flag flip); the hub uses `SettingsCategoryPage("Player")` (plain save, no gate, no flag flip).
5. `ConfirmDialogPage` exists under `Pages/ConfirmDialog/` (`.cs` + `.tscn`), is a generalised `MessagePage` (message Label + 3 buttons), and drives a 3-way decision via callbacks: Reset changes (commit the draft, then back), Back (stay on the category page), Continue (discard the draft, then back).
6. The old flat `SettingsPage.cs` + `SettingsPage.tscn` are **deleted**; `PagesProvider.SettingsPageScene` is removed; `MainPage.SettingsButton` now GoNext's to the hub. `MainMenu.tscn`'s `PagesProvider` node loses the `SettingsPageScene` assignment and gains the hub + category + dialog scene assignments.
7. New locale keys present in `messages.pot`, `en.po`, `ru.po` (Task 7.5).
8. `dotnet build` succeeds with **0 errors**.
9. No `// TODO` markers reference Phase 7; none were left by earlier phases for this work (verified by grep).

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Categories already exist in the model.** `MenuGameSettings` (`Scenes/Screen/NewMenu/SettingsSystem/MenuGameSettings.cs`) tags every property with `[Category(...)]`. Distinct categories present: `Player`, `Audio`, `Graphics`, `Interface`. **`Controls` has zero properties** — `SettingsCategoryPage("Controls")` renders an empty list. This is expected and required by the spec (the category page is a placeholder until controls are added later). The empty case must not crash.
- **The reflection pipeline already supports per-category rendering.** `GameSettingsBase.cs` (`partial class MenuGameSettings`) defines:
  - `public IReadOnlyList<Setting> GetVisibleSettings()` — all visible (no `[Hide]`) settings.
  - `public IReadOnlyList<Setting> GetVisibleSettings(string category)` — filtered by `[Category]` equality.
  - `public void SetVisibleSettings(IReadOnlyList<Setting> settings)` — `setting.Apply()` for each.
  - `public void SetVisibleSettings(IReadOnlyList<Setting> settings, string category)` — delegates to the no-arg (category ignored; safe because the list already contains only that category's settings).
  - `public string Serialize()` — `JsonSerializer.Serialize(this, GetType(), JsonSerializerOptions)` with `WriteIndented = true` and `ColorJsonConverter`. **Use this for the dirty check** — never hand-compare properties.
  Settings bind to the draft instance via `Setting.Target`; `SetVisibleSettings` writes `Setting.Value` back onto the draft. This is the exact pattern `SettingsPage.cs` uses today.
- **Dirty check = JSON string compare.** Snapshot `_preservedSettings = Services.MenuGameSettings.GetSettings()` at `_Ready`; on Back compare `_draftSettings.Serialize() != _preservedSettings.Serialize()`. Identical JSON → clean; any difference → dirty. The `ColorJsonConverter` makes `Color` round-trip deterministically, so color edits are detected. This is the brain-decided approach (general plan §7.5; brain page `main-menu-structure`).
- **Navigation primitives.** `Page.GoNext` / `Page.GoBack` are `protected Action<IPage>` / `protected Action` (see `Page.cs`). `PageContainer.PushPage`/`PopPage` operate on `container.CurrentPage`, **not** on `this` — so a confirm dialog pushed from a category page can call `GoBack` to pop itself, and a second `GoBack` from the category page pops the category. `MainMenuPage` (the base class for every page here) already exposes both plus `protected PagesProvider PagesProvider`. No new navigation plumbing.
- **`SettingsHubPage` visual template = `MultiplayerPage`.** Copy `Pages/Multiplayer/MultiplayerPage.cs` + `.tscn` node-for-node: full-rect `Control` → `MarginContainer` (margins 110/25/25/25) → `VBoxContainer` (`custom_minimum_size = Vector2(450,0)`, h+vert centered) → Title `Label` (`MULTIPLAYER_MENU__TITLE`-style key, `LabelSettings` Play-Bold 32 + cyan outline) + `Spacer` Label + 5 category `Button`s (each `custom_minimum_size = Vector2(0,35)`, neon `StyleBoxEmpty` normal + cyan `GradientTexture1D` hover, Play-Bold 18) + `Spacer2` + `BackButton`. The exact button styling block is repeated per-button in `MultiplayerPage.tscn` — duplicate it; do not factor it into a theme (the project doesn't use one for menu buttons).
- **`SettingsCategoryPage` visual template = `SettingsPage.tscn`.** The flat settings form shell is already exactly what a category page wants: `Control` full-rect → `MarginContainer` (20) → `PanelContainer` (`custom_minimum_size = Vector2(850,0)`, h-centered) → inner `MarginContainer` (20) → `VBoxContainer` (Title `Label` `font_size 32` centered → `ScrollContainer` (v-expand) → inner `MarginContainer` (10) → `SettingsContainer` `VBoxContainer` → outer `MarginContainer` → button `HBoxContainer` (h-centered) with Save + Cancel). Copy this skeleton; do not invent new layout. The only title change is the `text` key (per category).
- **`ConfirmDialogPage` visual template = `MessagePage`.** `MessagePage.tscn` is a centered `PanelContainer` (`custom_minimum_size = Vector2(700,350)`, center anchors 8, offsets ±20) → `MarginContainer` (20) → `VBoxContainer` (v-centered) → `MessageLabel` (h-centered) + `Separator` Label + single `OkButton` (`custom_minimum_size = Vector2(300,50)`, h-centered, text `MESSAGE_MENU__OK_BUTTON`). For the confirm dialog: keep the shell, change the button row to **three** buttons (`ResetButton`, `BackButton`, `ContinueButton`) in an `HBoxContainer` (h-centered), and the message Label becomes the dirty-warning text.
- **`[Child]` resolves by node name regardless of depth.** KludgeBox ChildInjection scans the subtree by name; depth doesn't matter. Node names in the `.tscn` must match `[Child]` property names **exactly** (case-sensitive). A mismatch compiles but throws at `_Ready`.
- **`[Export][NotNull]` PackedScene slots throw on launch if empty.** A missed editor assignment passes `dotnet build` and crashes at runtime (`PagesProvider._Ready` → `Di.Process` → `NotNullCheck`). After editing `MainMenu.tscn`, every new scene slot must be filled. This is the blocking runtime check for this phase.
- **Locale convention.** `SECTION__KEY`, three files: `Assets/Locales/messages.pot` (empty `msgstr`), `en.po`, `ru.po`. `Tr()` is identity for unknown keys, so a missing translation renders the key verbatim — but all keys must be added to all three files. Existing reusable keys: `GENERIC_MENU__SAVE_BUTTON`, `GENERIC_MENU__CANCEL_BUTTON`, `GENERIC_MENU__BACK_BUTTON` (all present). `SETTING_MENU__NICK`/`COLOR`/`*_HINT`/volumes/resolution/interface-size/fullscreen are all present (Phase 1) — the `SettingContainer` pipeline reads them via `[Name]`/`[Hint]`; do NOT re-add.
- **Player color upgrade is free.** Phase 6 swapped the `Color` configurator to `ColorPickerPanel`. `SettingsCategoryPage("Player")` renders `PlayerColor` through `SettingContainer`, so it gets the new panel automatically — no extra work.
- **`PlayerSettingsPage` (Phase 5 gate) stays as-is.** Do NOT delete it, do NOT redirect the gate to the hub. The gate's special behaviour (continuation-action after save, `PlayerSettingsAcknowledged = true` flip) is orthogonal to editing settings from the hub. The hub's Player category is a plain save. Both render the Player category through `SettingContainer`; they differ only in what Save does. This matches the brain page's explicit allowance ("если усложнит — разделить на 2 класса").

---

## Task 7.1 — `SettingsHubPage`

### 7.1a — `Scenes/Screen/NewMenu/MainMenu/Pages/SettingsHub/SettingsHubPage.cs` (new)

Copy the shape of `MultiplayerPage.cs`. Six `[Child] Button`s (5 categories + Back). The category buttons each GoNext a `SettingsCategoryPage` prepared with a `(category, titleKey)` pair.

```csharp
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

        PlayerButton.Pressed += () => GoNext(PagesProvider.PrepareSettingsCategoryPage("Player", "SETTINGS_HUB__PLAYER_TITLE"));
        ControlsButton.Pressed += () => GoNext(PagesProvider.PrepareSettingsCategoryPage("Controls", "SETTINGS_HUB__CONTROLS_TITLE"));
        InterfaceButton.Pressed += () => GoNext(PagesProvider.PrepareSettingsCategoryPage("Interface", "SETTINGS_HUB__INTERFACE_TITLE"));
        GraphicsButton.Pressed += () => GoNext(PagesProvider.PrepareSettingsCategoryPage("Graphics", "SETTINGS_HUB__GRAPHICS_TITLE"));
        AudioButton.Pressed += () => GoNext(PagesProvider.PrepareSettingsCategoryPage("Audio", "SETTINGS_HUB__AUDIO_TITLE"));
        BackButton.Pressed += GoBack;
    }
}
```

Decisions baked in:

- **Six buttons, hardcoded.** Five categories are fixed by the spec (#12); a data-driven list adds indirection for no gain. Order: Player, Controls, Interface, Graphics, Audio (matches the spec's listing order and the brain page's target structure).
- **Title keys live under `SETTINGS_HUB__*`, not `SETTING_MENU__*`.** The hub is a new screen; its section prefix is `SETTINGS_HUB__`. The category *page titles* reuse `SETTINGS_HUB__<CAT>_TITLE` so one prefix covers the whole hub tree. (The individual setting labels inside each category page still come from the existing `SETTING_MENU__*` keys via `[Name]`/`[Hint]` — never re-add those.)
- **`GoBack` as a method group** (`BackButton.Pressed += GoBack;`) matches `MessagePage`'s idiom. `Page.GoBack` is `protected Action`; the method-group conversion is implicit.

### 7.1b — `Scenes/Screen/NewMenu/MainMenu/Pages/SettingsHub/SettingsHubPage.tscn` (new)

Duplicate `Pages/Multiplayer/MultiplayerPage.tscn` in the Godot editor (FileSystem dock → right-click → Duplicate, move into `Pages/SettingsHub/`, rename `SettingsHubPage.tscn`). Then:

1. Reattach the script to `SettingsHubPage.cs` (`[ext_resource]` path → `res://Scenes/Screen/NewMenu/MainMenu/Pages/SettingsHub/SettingsHubPage.cs`).
2. Rename the root node `MultiplayerPage` → `SettingsHubPage`.
3. Title Label `text`: `MULTIPLAYER_MENU__TITLE` → `SETTINGS_HUB__TITLE`.
4. Replace the three middle buttons (`CreateNewServerButton`/`CreateFromSaveButton`/`ConnectButton`) with five: `PlayerButton`, `ControlsButton`, `InterfaceButton`, `GraphicsicsButton`, `AudioButton`. Keep the exact per-button styling block from `MultiplayerPage.tscn` (the neon `StyleBoxEmpty` normal + `GradientTexture1D` hover + Play-Bold 18). Set each `text`:
   - `PlayerButton` → `SETTINGS_HUB__PLAYER_BUTTON`
   - `ControlsButton` → `SETTINGS_HUB__CONTROLS_BUTTON`
   - `InterfaceButton` → `SETTINGS_HUB__INTERFACE_BUTTON`
   - `GraphicsButton` → `SETTINGS_HUB__GRAPHICS_BUTTON`
   - `AudioButton` → `SETTINGS_HUB__AUDIO_BUTTON`
5. `BackButton` `text` stays `GENERIC_MENU__BACK_BUTTON`.

Node names must match the `[Child]` properties in 7.1a **exactly** (case-sensitive): `PlayerButton`, `ControlsButton`, `InterfaceButton`, `GraphicsButton`, `AudioButton`, `BackButton`.

Layout sketch (final):

```
SettingsHubPage (Control, full-rect, script=SettingsHubPage.cs)
└─ MarginContainer (margins 110/25/25/25)
   └─ VBoxContainer (custom_minimum_size 450x0, h-center, v-center)
      ├─ Title Label  text="SETTINGS_HUB__TITLE"  (LabelSettings: Play-Bold 32, cyan outline)
      ├─ Spacer Label
      ├─ PlayerButton      (neon style, text=SETTINGS_HUB__PLAYER_BUTTON)
      ├─ ControlsButton    (neon style, text=SETTINGS_HUB__CONTROLS_BUTTON)
      ├─ InterfaceButton   (neon style, text=SETTINGS_HUB__INTERFACE_BUTTON)
      ├─ GraphicsButton    (neon style, text=SETTINGS_HUB__GRAPHICS_BUTTON)
      ├─ AudioButton       (neon style, text=SETTINGS_HUB__AUDIO_BUTTON)
      ├─ Spacer2 Label
      └─ BackButton        (neon style, text=GENERIC_MENU__BACK_BUTTON)
```

---

## Task 7.2 — Generic `SettingsCategoryPage`

### 7.2a — `Scenes/Screen/NewMenu/MainMenu/Pages/SettingsCategory/SettingsCategoryPage.cs` (new)

The shared category editor. One class, five instances. Renders a category's visible settings, supports Save / Cancel, and on Back with dirty state opens the confirm dialog.

```csharp
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
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
        // Discard: re-apply the preserved snapshot to undo any runtime side-effects of edits
        // (volume sliders apply live through the configurators' ValueChanged handlers? — they do
        // NOT; edits write to Setting.Value until ApplyAndSaveSettings. But re-applying is still
        // correct and harmless), then return.
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
```

Decisions baked in:

- **One generic class, configured at prepare time.** `Configure(category, titleKey)` is called by `PagesProvider.PrepareSettingsCategoryPage` **before** the page enters the tree (same pattern as `PlayerSettingsPage.SetContinuation`). `_Ready` then reads `_category`/`_titleKey`. Because `PageContainer.PushPage` calls `AddChild` after the page is prepared, `_Ready` runs after `Configure` — order is guaranteed.
- **Save / Cancel / Back are three distinct buttons.** The flat `SettingsPage` had Save + Cancel only. The spec (#12) calls for an unsaved-changes dialog on Back, so Back is its own button here. Save = commit + leave; Cancel = discard + leave (identical to a clean Back); Back = leave, but prompt if dirty. **If you want to collapse Cancel into Back** (since Cancel ≡ clean Back), you may remove `CancelButton` and its `[Child]` + node — but the spec's hub design implies Save + Back; pick Save + Back to keep the form minimal. **Default: keep all three** (Save/Cancel/Back) for clarity; the scene in 7.2b includes all three. The implementer may drop Cancel if the hub's category pages should mirror a simpler Save/Back form — note the choice in the report.
- **`OnSave` refreshes `_preservedSettings` after persisting.** Without this, a Save followed by an immediate Back (no further edits) would still see `IsDirty() == true` (the draft was mutated in-place by the configurators before Save committed it, but `_preservedSettings` still holds the pre-edit snapshot). Re-reading from the service post-save makes the just-saved state the new clean baseline. This is the one non-obvious line; do not delete it.
- **`OnCancel` re-applies the preserved snapshot.** Edits wrote to `Setting.Value` (bound to `_draftSettings`), not to live settings, until `ApplyAndSaveSettings`. Re-applying preserved is correct and harmless; it mirrors `SettingsPage.OnCancel` and `PlayerSettingsPage.OnCancel` exactly.
- **`IsDirty()` compares full serialized JSON.** Catches every changed field in the category (and only the category, because the draft and preserved were both loaded from the same source — the only differences are the in-category edits). The `ColorJsonConverter` makes `Color` round-trip deterministically. Simple, robust, brain-decided.
- **Confirm dialog callbacks are closures over `this`.** `onReset`/`onContinue` capture the category page and call its `GoBack`/`SetVisibleSettings`/`ApplyAndSaveSettings`. Because `GoBack` pops `container.CurrentPage`, and the confirm dialog is the current page when a button is pressed, the dialog pops itself first; the category page's `GoBack` (called inside the callback) then pops the category page. **Order matters:** the callback runs *inside* the dialog's button handler, so the dialog must pop itself before the category page pops itself. `ConfirmDialogPage` (Task 7.4) handles this by calling `GoBack` (pops the dialog) and *then* invoking the callback (which pops the category). See 7.4a for the exact sequence.
- **No `Validate()` call.** `SettingsPage` and `PlayerSettingsPage` do not call it. `MenuGameSettings.Validate()` only null-coalesces three nullable strings; the configurators cannot set them to null. Skip it for parity.
- **Empty category ("Controls") is handled.** `GetVisibleSettings("Controls")` returns an empty list; the `foreach` adds nothing; the scroll area is empty; Save/Cancel/Back all work (Save is a clean no-op commit). No special-casing.

### 7.2b — `Scenes/Screen/NewMenu/MainMenu/Pages/SettingsCategory/SettingsCategoryPage.tscn` (new)

Duplicate `Pages/Settings/SettingsPage.tscn` (the flat settings form shell) as the starting point, then:

1. Reattach the script to `SettingsCategoryPage.cs`.
2. Rename the root node `SettingsPage` → `SettingsCategoryPage`.
3. The Title Label node: rename to `Title` (or keep `Settings` — doesn't matter; it's bound by `[Child] TitleLabel`, so **rename the node to `TitleLabel`** to match the `[Child]` property). Keep `label_settings` (`font_size = 32`); the `text` will be set from code (`TitleLabel.Text = Tr(_titleKey)`), so the scene's `text` value is a placeholder — set it to `SETTINGS_HUB__PLAYER_TITLE` for editor preview, it'll be overwritten at runtime.
4. The button `HBoxContainer` currently has `SaveButton` + `CancelButton`. **Add a third button** `BackButton` (same `custom_minimum_size = Vector2(200,50)`), text `GENERIC_MENU__BACK_BUTTON`.

Node names must match the `[Child]` properties in 7.2a **exactly**: `TitleLabel`, `SettingsContainer`, `SaveButton`, `CancelButton`, `BackButton`.

Layout sketch (final):

```
SettingsCategoryPage (Control, full-rect, script=SettingsCategoryPage.cs)
└─ MarginContainer (margin 20)
   └─ PanelContainer (custom_minimum_size 850x0, h-center)
      └─ MarginContainer (margin 20)
         └─ VBoxContainer
            ├─ TitleLabel  Label  (label_settings font_size 32, h-center)  [text set from code]
            ├─ ScrollContainer (size_flags_vertical = 3)
            │   └─ MarginContainer (margin 10, h+v expand)
            │      └─ SettingsContainer  VBoxContainer
            └─ MarginContainer
               └─ HBoxContainer (h-center, v-center)
                  ├─ SaveButton    text=GENERIC_MENU__SAVE_BUTTON    (custom_minimum_size 200x50)
                  ├─ CancelButton  text=GENERIC_MENU__CANCEL_BUTTON  (custom_minimum_size 200x50)
                  └─ BackButton    text=GENERIC_MENU__BACK_BUTTON    (custom_minimum_size 200x50)
```

---

## Task 7.3 — `PagesProvider`: scene slots + prepare helpers

### 7.3a — `Scenes/Screen/NewMenu/MainMenu/PagesProvider.cs` (edit)

**Remove** the `SettingsPageScene` field (the flat settings page is being deleted). **Add** three new scene slots + two prepare helpers.

Current relevant fields:
```csharp
[Export] [NotNull] public PackedScene SettingsPageScene { get; private set; }
[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
[Export] [NotNull] public PackedScene PlayerSettingsPageScene { get; private set; }
```

After:
```csharp
[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
[Export] [NotNull] public PackedScene PlayerSettingsPageScene { get; private set; }
[Export] [NotNull] public PackedScene SettingsHubPageScene { get; private set; }
[Export] [NotNull] public PackedScene SettingsCategoryPageScene { get; private set; }
[Export] [NotNull] public PackedScene ConfirmDialogPageScene { get; private set; }
```

(Delete the `SettingsPageScene` line entirely.)

Add two prepare helpers (alongside `PreparePlayerSettingsPage`):

```csharp
public Pages.SettingsCategory.SettingsCategoryPage PrepareSettingsCategoryPage(string category, string titleKey)
{
    var page = SettingsCategoryPageScene.Instantiate<Pages.SettingsCategory.SettingsCategoryPage>().WithAvailablePages(this);
    page.Configure(category, titleKey);
    return page;
}

public Pages.ConfirmDialog.ConfirmDialogPage PrepareConfirmDialogPage(string message, Action onReset = null, Action onContinue = null, Action onBack = null)
{
    var page = ConfirmDialogPageScene.Instantiate<Pages.ConfirmDialog.ConfirmDialogPage>().WithAvailablePages(this);
    page.Setup(message, onReset, onContinue, onBack);
    return page;
}
```

Decisions baked in:

- **`PrepareSettingsCategoryPage` is a dedicated helper, not the generic `PreparePage`.** Like `PreparePlayerSettingsPage`, it must call `Configure(...)` before the page enters the tree. The generic `PreparePage(PackedScene)` cannot do that. `WithAvailablePages(this)` is the `MainMenuPageExtensions` helper that calls `SetPagesProvider`.
- **`PrepareConfirmDialogPage` takes three optional callbacks.** `onReset` and `onContinue` are wired to the dialog's Reset and Continue buttons; `onBack` (the "stay here" button) is optional — when null, the dialog's Back button just pops the dialog (no callback). This makes the dialog reusable for non-dirty-confirm flows later.
- **The flat `SettingsPageScene` slot is removed.** The flat `SettingsPage` is deleted in Task 7.6. Keeping a dangling `[NotNull]` slot would crash on launch.

### 7.3b — `MainPage.cs` (edit) — repoint `SettingsButton` to the hub

Current:
```csharp
SettingsButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SettingsPageScene));
```
After:
```csharp
SettingsButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.SettingsHubPageScene));
```

The hub is a plain neon-button page (no pre-config), so the generic `PreparePage` is fine — same as how `MultiplayerButton` uses `PreparePage(PagesProvider.MultiplayerPageScene)`. No new prepare helper for the hub.

### 7.3c — `MainMenu.tscn` (edit) — rewire the `PagesProvider` node

This is the blocking editor step. Open `MainMenu.tscn` in the Godot editor, select the `PagesProvider` node, and in the Inspector:

1. **Remove** the `SettingsPageScene` assignment (the export field is gone from C#; the editor will drop it). Also remove the corresponding `[ext_resource]` line for `SettingsPage.tscn` (id `5_h4gti`) — the editor does this automatically when the resource is no longer referenced, but verify after saving.
2. **Add** three new assignments by dragging the new `.tscn` files into the new export slots:
   - `SettingsHubPageScene` ← `Pages/SettingsHub/SettingsHubPage.tscn`
   - `SettingsCategoryPageScene` ← `Pages/SettingsCategory/SettingsCategoryPage.tscn`
   - `ConfirmDialogPageScene` ← `Pages/ConfirmDialog/ConfirmDialogPage.tscn`
3. Save. The editor rewrites `load_steps`, `uid`/`path`/`id`, and ext-resource entries correctly.

After saving, the `MainMenu.tscn` header `load_steps` changes (−1 removed SettingsPage ext_resource, +3 new ones → net +2; verify in the file). The `PagesProvider` node block ends with three new `*Scene = ExtResource("<id>")` lines and no `SettingsPageScene` line.

> **Blocking runtime check.** All three new slots are `[NotNull]`. An empty slot passes `dotnet build` but throws on launch. After the editor save, confirm all three slots are filled before the verify step.

---

## Task 7.4 — `ConfirmDialogPage`

### 7.4a — `Scenes/Screen/NewMenu/MainMenu/Pages/ConfirmDialog/ConfirmDialogPage.cs` (new)

A generalised `MessagePage`: a message Label + three buttons (Reset / Back / Continue). The three-way decision is driven by callbacks supplied at prepare time.

```csharp
using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.ConfirmDialog;

public partial class ConfirmDialogPage : MainMenuPage
{
    [Child] public Label MessageLabel { get; private set; }
    [Child] public Button ResetButton { get; private set; }
    [Child] public Button BackButton { get; private set; }
    [Child] public Button ContinueButton { get; private set; }

    private Action _onReset;
    private Action _onContinue;
    private Action _onBack;

    public override void _Ready()
    {
        Di.Process(this);

        ResetButton.Pressed += OnResetPressed;
        BackButton.Pressed += OnBackPressed;
        ContinueButton.Pressed += OnContinuePressed;
    }

    /// <summary>Called by <see cref="PagesProvider.PrepareConfirmDialogPage"/> before the page is added to the tree.</summary>
    public void Setup(string message, Action onReset, Action onContinue, Action onBack)
    {
        // MessageLabel.Text must be set in _Ready (the node doesn't exist yet at Setup time);
        // stash the message and apply it after Di.Process.
        _message = message;
        _onReset = onReset;
        _onContinue = onContinue;
        _onBack = onBack;
    }

    private string _message;

    public override void _Ready()
    {
        Di.Process(this);

        MessageLabel.Text = _message;

        ResetButton.Pressed += OnResetPressed;
        BackButton.Pressed += OnBackPressed;
        ContinueButton.Pressed += OnContinuePressed;
    }

    private void OnResetPressed()
    {
        // "Reset changes" — but the button label in the spec is "Reset changes". Semantics here:
        // commit the draft (caller's onReset does SetVisibleSettings + ApplyAndSaveSettings),
        // then close the dialog.
        _onReset?.Invoke();
        GoBack(); // pop the dialog
    }

    private void OnContinuePressed()
    {
        // "Continue" (a.k.a. discard) — caller's onContinue re-applies the preserved snapshot.
        _onContinue?.Invoke();
        GoBack(); // pop the dialog
    }

    private void OnBackPressed()
    {
        // "Back" — stay on the category page (just close the dialog). If a caller supplied an
        // onBack (rare), run it; otherwise this is a plain dialog pop.
        _onBack?.Invoke();
        GoBack(); // pop the dialog
    }
}
```

> **Note on the duplicated `_Ready` above:** the snippet shows `_Ready` twice to illustrate the message-stash fix — that is a mistake in this plan, not a pattern to copy. The correct file has **exactly one `_Ready`** that does `Di.Process(this); MessageLabel.Text = _message;` then wires the three handlers. Delete the first `_Ready` block (the one without `MessageLabel.Text = _message;`). The final file has one `_Ready`, one `Setup`, three `On*Pressed` methods, four fields (`_message`, `_onReset`, `_onContinue`, `_onBack`), four `[Child]` properties. (Left visible as a gotcha — do not carry the duplicate into the code.)

**CRITICAL — callback / pop ordering with the category page.** Re-read this before writing `OnBack` in `SettingsCategoryPage` (7.2a). The flow for "Reset changes" on a dirty category page:

1. User on category page, has edits, clicks **Back** → `SettingsCategoryPage.OnBack` → `IsDirty()` true → `GoNext(PrepareConfirmDialogPage(... onReset: () => { commit; GoBack(); } ...))`. The confirm dialog is now `CurrentPage`; the category page is its parent (removed from tree, kept in memory).
2. User clicks **Reset changes** → `ConfirmDialogPage.OnResetPressed` → invokes `onReset` (the category page's closure: `SetVisibleSettings` + `ApplyAndSaveSettings` + `GoBack`).
3. **Inside `onReset`, the category page calls `GoBack`.** But `GoBack` pops `container.CurrentPage`, which is the **dialog**, not the category page. So the category page's `GoBack` pops the dialog.
4. Control returns to `OnResetPressed`, which then calls `GoBack()` **again** — but now `CurrentPage` is the category page (the dialog was just popped), so this second `GoBack` pops the category page. ✅ Both pop.

So the closure's `GoBack` pops the dialog, and `OnResetPressed`'s own `GoBack` pops the category. **Do not remove either `GoBack` call.** If you find the dialog pops but the category page stays, you are missing one of the two. Verify in the editor run (Task 7.6 step 5).

Decisions baked in:

- **Three buttons, not two.** The spec (general plan §7.4) explicitly lists Reset changes / Back / Continue. Reset = commit + leave; Back = stay (close dialog only); Continue = discard + leave. The labels are deliberately three distinct actions.
- **Callbacks optional.** `Setup(message, onReset, onContinue, onBack)` — any may be null. A null `onBack` makes the Back button a plain dialog pop (the common case for the dirty-confirm flow, where "stay on category page" needs no extra action). A null `onReset`/`onContinue` makes those buttons just close the dialog (rare; the dirty-confirm flow always supplies both).
- **Generalises `MessagePage`.** `MessagePage` is message + 1 button (OK = back). `ConfirmDialogPage` is message + 3 buttons. They are separate classes (not a refactor of `MessagePage`) to avoid touching `MessagePage`'s callers. If you want to unify later, that's a separate refactor — out of scope.
- **`MessageLabel.Text` set in `_Ready`, not `Setup`.** `Setup` runs before the page enters the tree; the `[Child]` nodes don't exist yet (`Di.Process` hasn't run). Stash the message in a field, apply it in `_Ready` after `Di.Process`. This mirrors why `PlayerSettingsPage.SetContinuation` only stashes (it doesn't touch nodes) and why `SettingsCategoryPage.Configure` only stashes.
- **The dialog does NOT free the category page.** `PageContainer.PopPage` re-adds the parent to the tree and frees the popped child (the dialog). The category page (parent) survives and becomes visible again when the dialog pops via "Back". For "Reset"/"Continue", the category page's own `GoBack` (in the closure) then pops and frees it.

### 7.4b — `Scenes/Screen/NewMenu/MainMenu/Pages/ConfirmDialog/ConfirmDialogPage.tscn` (new)

Duplicate `Pages/Message/MessagePage.tscn` as the starting point, then:

1. Reattach the script to `ConfirmDialogPage.cs`.
2. Rename the root node `MessagePage` → `ConfirmDialogPage`.
3. Rename `OkButton` → keep it but turn the single-button row into a three-button `HBoxContainer`. Simplest: add an `HBoxContainer` as a sibling of `OkButton`'s parent position, or replace the single button with three. Concrete structure:

```
ConfirmDialogPage (Control, full-rect, script=ConfirmDialogPage.cs)
└─ PanelContainer (custom_minimum_size 700x350, center anchors 8, offsets ±20)
   └─ MarginContainer (margin 20)
      └─ VBoxContainer (size_flags_vertical = 4)
         ├─ MessageLabel  Label  (h-center, text set from code)  [placeholder: CONFIRM_DIALOG__UNSAVED_CHANGES]
         ├─ Separator  Label
         └─ HBoxContainer (size_flags_horizontal = 4, h-center)
            ├─ ResetButton     text=CONFIRM_DIALOG__RESET_BUTTON     (custom_minimum_size 200x50)
            ├─ BackButton      text=CONFIRM_DIALOG__BACK_BUTTON      (custom_minimum_size 200x50)
            └─ ContinueButton  text=CONFIRM_DIALOG__CONTINUE_BUTTON  (custom_minimum_size 200x50)
```

Node names must match the `[Child]` properties in 7.4a **exactly**: `MessageLabel`, `ResetButton`, `BackButton`, `ContinueButton`.

Keep the `PanelContainer` center-anchored layout from `MessagePage.tscn` (anchors preset 8, `offset_left/top = -20`, `offset_right/bottom = 20`, `grow_horizontal/vertical = 2`) so the dialog appears centered over the category page. Three buttons at `200x50` each fit within the 700-wide panel.

---

## Task 7.5 — Translation keys

Add **every** key below to **all three** files: `Assets/Locales/messages.pot` (`msgstr` empty), `Assets/Locales/en.po`, `Assets/Locales/ru.po`. Follow the existing format (blank line between entries, `msgid` then `msgstr`). Place the new block after the existing `PLAYER_SETTINGS_MENU__TITLE` entry (the tail of each file).

| msgid | en msgstr | ru msgstr |
|---|---|---|
| `SETTINGS_HUB__TITLE` | `Settings` | `Настройки` |
| `SETTINGS_HUB__PLAYER_BUTTON` | `Player` | `Игрок` |
| `SETTINGS_HUB__CONTROLS_BUTTON` | `Controls` | `Управление` |
| `SETTINGS_HUB__INTERFACE_BUTTON` | `Interface` | `Интерфейс` |
| `SETTINGS_HUB__GRAPHICS_BUTTON` | `Graphics` | `Графика` |
| `SETTINGS_HUB__AUDIO_BUTTON` | `Audio` | `Аудио` |
| `SETTINGS_HUB__PLAYER_TITLE` | `Player` | `Игрок` |
| `SETTINGS_HUB__CONTROLS_TITLE` | `Controls` | `Управление` |
| `SETTINGS_HUB__INTERFACE_TITLE` | `Interface` | `Интерфейс` |
| `SETTINGS_HUB__GRAPHICS_TITLE` | `Graphics` | `Графика` |
| `SETTINGS_HUB__AUDIO_TITLE` | `Audio` | `Аудио` |
| `CONFIRM_DIALOG__UNSAVED_CHANGES` | `You have unsaved changes.` | `У вас есть несохранённые изменения.` |
| `CONFIRM_DIALOG__RESET_BUTTON` | `Reset changes` | `Сбросить изменения` |
| `CONFIRM_DIALOG__BACK_BUTTON` | `Back` | `Назад` |
| `CONFIRM_DIALOG__CONTINUE_BUTTON` | `Continue` | `Продолжить` |

Notes:
- `SETTINGS_HUB__<CAT>_BUTTON` and `SETTINGS_HUB__<CAT>_TITLE` are deliberately distinct keys even when their translations are identical (e.g. `Player`/`Player`). The button label and the page title may diverge later (button might get an icon or shorter label); keeping them separate avoids a future rename. Do not collapse them.
- `CONFIRM_DIALOG__BACK_BUTTON` is a distinct key from `GENERIC_MENU__BACK_BUTTON` even though both translate to `Back`/`Назад`. The confirm dialog's Back is a *different action* (stay on page) from a generic Back (navigate out); keep them separate for the same future-divergence reason.
- Reuse, do NOT re-add: `GENERIC_MENU__SAVE_BUTTON`, `GENERIC_MENU__CANCEL_BUTTON`, `GENERIC_MENU__BACK_BUTTON`, and all `SETTING_MENU__*` keys (the `SettingContainer` pipeline reads them via `[Name]`/`[Hint]`).
- The placeholder `text` values in the new `.tscn` files (e.g. `CONFIRM_DIALOG__UNSAVED_CHANGES` on the dialog's `MessageLabel`, `SETTINGS_HUB__PLAYER_TITLE` on the category page's `TitleLabel`) are editor-preview only and overwritten at runtime — but they must be valid keys present in the locale files, or Godot's editor translation preview shows the raw key. Adding them in this task covers that.

---

## Task 7.6 — Delete the flat `SettingsPage`; verify

### 7.6a — Delete the flat settings page

1. Delete `Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingsPage.cs`.
2. Delete `Scenes/Screen/NewMenu/MainMenu/Pages/Settings/SettingsPage.tscn`.
3. Delete their `.uid` sidecar files (`SettingsPage.cs.uid`, `SettingsPage.tscn.uid`) if present.
4. **Do NOT delete** `Pages/Settings/SettingContainer.cs` — it stays (the category pages render through it). The `Settings/` directory keeps `SettingContainer.cs` (+ `.uid`).

After deletion, grep to confirm no surviving references to the flat page:
```bash
grep -rn "SettingsPage\b\|SettingsPageScene" --include=*.cs --include=*.tscn .
```
Expect: **zero hits** (the `SettingsPage` class, the `SettingsPageScene` field, the `.tscn` ext-resource in `MainMenu.tscn`, and `MainPage`'s `SettingsPageScene` reference are all gone). `SettingsCategoryPage` / `SettingsHubPage` are different names and won't match `\bSettingsPage\b`. If any hit remains, it's a missed reference — fix before building.

### 7.6b — Build

```bash
dotnet build
```
Expect: **0 errors.** Common errors at this stage and their causes:
- `CS0246: type or namespace 'SettingsHubPage'/'SettingsCategoryPage'/'ConfirmDialogPage' not found` → a namespace typo in the `.cs` file or a missing `using` in `PagesProvider.cs`. The prepare helpers use fully-qualified `Pages.SettingsHub.SettingsHubPage` etc., so `PagesProvider.cs` needs no new `using`, but verify the new files' `namespace` matches the folder path exactly.
- `CS0103: name 'SettingsPageScene' does not exist` → a stale reference (the `MainPage.cs` edit in 7.3b or the `PagesProvider.cs` edit in 7.3a was missed). Re-check both.
- `CS0115: member not found override` → a `[Child]` property name doesn't match the `.tscn` node name; re-check 7.1b/7.2b/7.4b node names.
- Runtime `NotNullCheck` exception on launch → an empty `[Export] PackedScene` slot on the `PagesProvider` node in `MainMenu.tscn`. Re-open the editor, fill the slot, save.

### 7.6c — Editor verification (manual, blocking)

Open `MainMenu.tscn` in the Godot editor and confirm:

1. The `PagesProvider` node has `SettingsHubPageScene`, `SettingsCategoryPageScene`, `ConfirmDialogPageScene` assigned (no empty export slots), and **no** `SettingsPageScene` field. This is blocking: a missed assignment passes `dotnet build` but crashes on launch.
2. Run the menu (F5). From MainPage → **Settings** → the `SettingsHubPage` appears (5 neon buttons + Back), not the old flat list.
3. Click each category button:
   - **Player** → `SettingsCategoryPage("Player")` renders nick + color (now the Phase-6 `ColorPickerPanel`) + uid + autosave. Edit the color via the new panel. Title shows `SETTINGS_HUB__PLAYER_TITLE`.
   - **Controls** → renders an empty scroll area (no settings). Title `SETTINGS_HUB__CONTROLS_TITLE`. Save/Back work.
   - **Interface** → renders the `InterfaceSize` `OptionButton` dropdown (Small/Medium/Large).
   - **Graphics** → renders `Fullscreen` checkbox + `Resolution` `OptionButton` dropdown.
   - **Audio** → renders the three volume `HSlider`s (0-100).
4. **Dirty-confirm flow:** open a category, make an edit (e.g. drag a volume slider), click **Back**. The `ConfirmDialogPage` appears with the unsaved-changes message and three buttons:
   - **Reset changes** → commits the edit, dialog closes, category page closes → back at hub. Re-open the category → the edit persisted.
   - **Back** → dialog closes, category page stays with the edit intact.
   - **Continue** → discards the edit, dialog closes, category page closes → back at hub. Re-open the category → the edit is gone.
5. **Save flow:** open a category, make an edit, click **Save** → category closes → back at hub. Re-open → edit persisted. **Then click Back immediately** → no confirm dialog (the post-save `_preservedSettings` refresh in 7.2a makes the just-saved state the clean baseline).
6. **Cancel flow:** open a category, make an edit, click **Cancel** → category closes, edit discarded. (Cancel ≡ clean Back; no confirm dialog because Cancel does not check dirty — it unconditionally discards.)
7. **First-run gate still works (regression):** delete/flip `PlayerSettingsAcknowledged` in `user://game-settings.json`, then MainPage → Singleplayer → Start → the **Phase-5 `PlayerSettingsPage`** gate appears (not the hub's Player category). Save → game starts. This confirms Phase 5's gate page was not broken by the Phase-7 changes.

---

## Out of scope for Phase 7 (do NOT do)

- **Phase 8 locale pass for non-Phase-7 keys** — Phase 8. This phase adds only the keys it needs (`SETTINGS_HUB__*`, `CONFIRM_DIALOG__*`).
- **Actual `Controls` settings (keybinds, mouse sensitivity, etc.)** — out of epic scope. The Controls category page is a placeholder; populating it is a later epic. The empty category page is the deliverable.
- **Real `AutoScale` (#137) for `InterfaceSize`** — out of epic scope. `InterfaceSize` is a stored dropdown value only; the scaling mechanism is separate.
- **Unifying `MessagePage` and `ConfirmDialogPage`** — separate classes on purpose; do not refactor `MessagePage` or its callers.
- **Redirecting the Phase-5 gate to the hub** — `PlayerSettingsPage` (gate) stays as-is. The hub's Player category is a separate, plain-save path. Both coexist.
- **Theming the neon buttons via a Godot `Theme` resource** — the project styles menu buttons per-instance via `theme_override_*`. Match that; do not introduce a theme resource.
- **Changing `SettingContainer`, `Configurators`, `MenuGameSettings`, `GameSettingsBase`, `MenuGameSettingsService`, `ColorPickerPanel`** — Phases 1 and 6 wired everything this phase needs. This phase only adds pages and rewires navigation.
- **Changing `Page`/`PageContainer`/`IPage`/`MainMenuPage`** — the push/pop primitives are sufficient. No new navigation plumbing.

---

## Gotchas recap

- **Delete the duplicate `_Ready` in 7.4a.** The snippet shows it twice to flag the message-stash fix; the real file has one `_Ready`. Carrying the duplicate is a compile error (duplicate override).
- **`OnSave` must refresh `_preservedSettings` after persisting.** Without it, Save-then-Back re-prompts the dirty dialog (the pre-edit snapshot is still the baseline). The one-line refresh makes the saved state the new clean baseline.
- **Confirm-dialog callback / pop ordering.** The category page's closure calls `GoBack` (pops the dialog, which is `CurrentPage`), then `OnResetPressed`/`OnContinuePressed` calls `GoBack` again (pops the category page). Both calls are required. If only the dialog pops and the category page stays, you're missing the second `GoBack`.
- **`IsDirty()` uses `Serialize()` (JSON string compare), not property-by-property.** The `ColorJsonConverter` makes `Color` deterministic. Don't hand-compare.
- **`[Child]` node names must match property names exactly (case-sensitive).** `TitleLabel`, `SettingsContainer`, `SaveButton`, `CancelButton`, `BackButton`, `ResetButton`, `ContinueButton`, `MessageLabel`, `PlayerButton`, `ControlsButton`, `InterfaceButton`, `GraphicsButton`, `AudioButton`.
- **`[Export][NotNull]` slots throw on launch if empty.** After editing `MainMenu.tscn`, all three new slots (`SettingsHubPageScene`, `SettingsCategoryPageScene`, `ConfirmDialogPageScene`) must be filled. The `SettingsPageScene` slot must be gone (field deleted in C#). This is the blocking runtime check.
- **`SettingsHubPage`/`MultiplayerPage` neon styling is per-button.** Copy the full `theme_override_colors`/`theme_override_fonts`/`theme_override_styles` block per button from `MultiplayerPage.tscn`. Do not factor it.
- **`Controls` category is intentionally empty.** `GetVisibleSettings("Controls")` returns `[]`. The page must render, save (no-op clean), and back without crashing. No special-case code.
- **Player color upgrade is free.** `SettingsCategoryPage("Player")` renders `PlayerColor` through `SettingContainer` → Phase-6 `ColorPickerPanel`. No extra work.
- **Phase-5 `PlayerSettingsPage` is untouched.** The gate still works (regression check in 7.6c step 7). The hub's Player category is a *new*, separate path.
- **`MainPage.SettingsButton` points to the hub, not the flat page.** The edit in 7.3b is one line; missing it leaves a dangling `SettingsPageScene` reference (compile error after the field is deleted).
- **`PrepareSettingsCategoryPage` calls `Configure` before `_Ready`.** `PageContainer.PushPage` adds the child after `PreparePage`/prepare-helper returns, so `Configure` runs first, `_Ready` reads the stashed fields. Do not move `Configure` logic into `_Ready` (the category/title aren't known at construction).
