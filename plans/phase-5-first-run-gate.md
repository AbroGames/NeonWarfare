# Phase 5 — First-run PlayerSettings gate (#16) (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 5 of 8. **Blocked by:** Phase 1 (`PlayerSettingsAcknowledged` field + `[Category("Player")]` exist), Phase 3 (`CreateNewServerPage`/`CreateSavedServerPage` exist with `// TODO (#16 gate, Phase 5)` markers), Phase 4 (`ServerListPage.OnConnectDirect` exists with the same marker). **Prerequisite for:** Phase 7 reuses this page's rendering pipeline for the Player category inside the Settings hub.

This phase adds the **first-run gate**: before any game start (singleplayer, host-new, host-from-save, connect), if `GameSettings.PlayerSettingsAcknowledged` is `false`, push a **`PlayerSettingsPage`** (nick + color, rendered through the existing `SettingContainer` pipeline). Saving on that page sets the flag to `true`, persists, then runs the deferred game-start action. Subsequent starts skip the page.

## Goal (done = all of)

1. `PlayerSettingsPage` exists under `Pages/PlayerSettings/` (`PlayerSettingsPage.cs` + `.tscn`), derives from `MainMenuPage`, renders the Player-category settings (nick + color + uid + autosave) via `SettingContainer`, and has Save / Cancel buttons. Save applies the draft, sets `PlayerSettingsAcknowledged = true`, persists, and runs a deferred continuation (the real game start); Cancel re-applies the preserved settings and returns with no game start.
2. A `TryStartGame(Action startAction)` protected helper exists on `MainMenuPage`: if acknowledged → run `startAction()` immediately; else → `GoNext(PagesProvider.PreparePlayerSettingsPage(startAction))`.
3. All **four** game-start call sites are wrapped in `TryStartGame(...)`:
   - `SingleplayerPage.OnStart` → `Services.MainScene.StartSingleplayerGame(saveFileName)`
   - `CreateNewServerPage.ParseAndStartServer` → `Services.MainScene.HostMultiplayerGameAsClient(...)`
   - `CreateSavedServerPage.OnCreate` → `Services.MainScene.HostMultiplayerGameAsClient(...)`
   - `ServerListPage.OnConnectDirect` → `Services.MainScene.ConnectToMultiplayerGame(...)`
4. `PagesProvider` gains a `[Export][NotNull] PackedScene PlayerSettingsPageScene` field + a `PreparePlayerSettingsPage(Action continuation)` helper; `MainMenu.tscn` assigns the new scene to the `PagesProvider` node.
5. New locale keys present in `messages.pot`, `en.po`, `ru.po`.
6. `dotnet build` succeeds with **0 errors**.
7. The three `// TODO (#16 gate, Phase 5)` markers are removed (replaced by the real `TryStartGame(...)` call).

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Navigation primitives.** `MainMenuPage : Page` (in `MainMenuPage.cs`). `Page` (abstract, `PagesSystem/Page.cs`) exposes `protected Action<IPage> GoNext` and `protected Action GoBack`, wired by `PageContainer` via `page.Setup(() => PopPage(), PushPage)`. `MainMenuPage` also holds `protected PagesProvider PagesProvider` (set by `WithAvailablePages(...)`). Because `GoNext`/`GoBack`/`PagesProvider` are all accessible from `MainMenuPage`, the gate helper lives there — **not** in a static utility (a static helper cannot reach the `protected` `GoNext`).
- **Game starts replace the whole menu.** Every `Services.MainScene.*` start method (`StartSingleplayerGame`, `HostMultiplayerGameAsClient`, `ConnectToMultiplayerGame`) instantiates a `Game` and calls `_mainSceneContainer.ChangeStoredNode(game)`, which frees the MainMenu tree (the gate page included). So the continuation just calls the service — **no manual menu cleanup, no `GoBack`** after starting.
- **The gate field already exists (Phase 1).** `GameSettings.PlayerSettingsAcknowledged` (`bool`, default `false`, positional record param) and `MenuGameSettings.PlayerSettingsAcknowledged` (`[Category("Player")] [Hide]`, default `false`). It round-trips through both `MenuGameSettingsService.Convert(...)` overloads and persists to `user://game-settings.json`. **Read** via `Services.GameSettings.GetSettings().PlayerSettingsAcknowledged`. **Write** by setting it on a `MenuGameSettings` draft then `Services.MenuGameSettings.ApplyAndSaveSettings(draft)` (the field is `[Hide]`, so it is NOT in `GetVisibleSettings("Player")` — set it on the draft object directly).
- **Render settings through the shared pipeline — do NOT hand-roll editors.** `MenuGameSettings.GetVisibleSettings(string category)` returns `IReadOnlyList<Setting>` (each `Setting` wraps an accessor bound to the draft instance). `new SettingContainer(setting)` builds the label + input (LineEdit for nick/uid, `ColorPickerButton`+hex label for color, CheckBox for autosave) and writes edits back into `setting.Value`. `draft.SetVisibleSettings(list)` then pushes each `setting.Value` onto the draft. This is exactly what `SettingsPage.cs` does (read it first — it is the canonical pattern). Reusing it here means **zero new editor code** and identical styling to the existing settings screen.
- **Color editor in Phase 5 = the stock `ColorPickerButton`.** The custom `ColorPickerPanel` is **Phase 6** (not done). The existing `Color` configurator in `Configurators` already renders a `ColorPickerButton` + hex `Label` inside `SettingContainer`, so rendering the Player category gives you a working color picker for free. Phase 6 later swaps the `Color` configurator project-wide, which upgrades this page automatically. **Do NOT build a custom color picker in Phase 5.**
- **`[Child]` resolves by node name.** Every `[Child] public X Foo` needs a node named exactly `Foo` in the `.tscn`. A mismatch compiles but throws at `_Ready` injection time.
- **Form shell template.** `PlayerSettingsPage` is a form (PanelContainer shell), not a hub. Copy the outer skeleton from `Pages/Settings/SettingsPage.tscn` node-for-node: `Control` full-rect → `MarginContainer` (margin 20) → `PanelContainer` (`custom_minimum_size = Vector2(850, 0)`, h-centered) → inner `MarginContainer` (margin 20) → `VBoxContainer` (Title Label font_size 32 centered, `ScrollContainer` → inner `MarginContainer` margin 10 → `SettingsContainer` VBox, button `HBoxContainer` h-centered with Save + Cancel). `SettingsPage.tscn` is the closest analog; mirror it.
- **Locale convention.** `SECTION__KEY`, three files: `Assets/Locales/messages.pot` (empty `msgstr`), `en.po`, `ru.po`. `Tr()` is identity for unknown keys, so a missing translation renders the key verbatim — but all keys must still be added to all three files. Existing reusable keys: `GENERIC_MENU__SAVE_BUTTON`, `GENERIC_MENU__CANCEL_BUTTON` (both already present). `SETTING_MENU__NICK` / `SETTING_MENU__COLOR` / `SETTING_MENU__NICK_HINT` / `SETTING_MENU__COLOR_HINT` etc. already exist (used by the auto-generated settings labels) — the `SettingContainer` pipeline pulls display names via the `[Name]`/`[Hint]` attributes, so you do **not** re-add them.
- **Marker gap to fix.** Phase 3/4 left `// TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...).` on exactly three handlers: `CreateNewServerPage.ParseAndStartServer`, `CreateSavedServerPage.OnCreate`, `ServerListPage.OnConnectDirect`. **`SingleplayerPage.OnStart` has no marker** (Phase 3 missed it), but the general plan (§5.3) and the brain decision both include singleplayer in the gate ("перед single/multi new/connect"). Wrap all four; remove the three markers when done.

---

## Task 5.1 — Gate helper on `MainMenuPage`

### 5.1a — `Scenes/Screen/NewMenu/MainMenu/MainMenuPage.cs` (edit)

Add one protected method. The file currently is:

```csharp
﻿using NeonWarfare.Scenes.Screen.NewMenu.PagesSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu;

public partial class MainMenuPage : Page
{
    protected PagesProvider PagesProvider;
    public void SetPagesProvider(PagesProvider availablePages)
    {
        PagesProvider = availablePages;
    }
}

public static class MainMenuPageExtensions { ... }  // unchanged
```

Add `TryStartGame` inside the `MainMenuPage` class body (after `SetPagesProvider`):

```csharp
    /// <summary>
    /// First-run gate (#16). Runs <paramref name="startAction"/> immediately when the player
    /// has already acknowledged their settings; otherwise pushes <see cref="Pages.PlayerSettings.PlayerSettingsPage"/>
    /// with <paramref name="startAction"/> as the post-save continuation.
    /// </summary>
    protected void TryStartGame(Action startAction)
    {
        if (Services.GameSettings.GetSettings().PlayerSettingsAcknowledged)
        {
            startAction();
            return;
        }
        GoNext(PagesProvider.PreparePlayerSettingsPage(startAction));
    }
```

Add `using System;` at the top of the file.

Decisions baked in:

- **Lives on `MainMenuPage`, not a static helper.** `GoNext` is `protected` on `Page`; only the page (or a subclass) can call it. `MainMenuPage` is the common base of all four start pages and already holds `PagesProvider`, so a single protected method serves all of them with no callback/`GoNext`-passing plumbing. This also matches the literal marker text `TryStartGame(...)` the prior phases left.
- **Reads the gate from `Services.GameSettings` (the persisted source of truth), not from `Services.MenuGameSettings`.** `MenuGameSettingsService.GetSettings()` builds a fresh `MenuGameSettings` each call; `GameSettings` is the underlying persisted record. Either works (they round-trip), but reading the persisted record is one indirection fewer and unambiguous about whose value gates the decision.
- **Name = `TryStartGame`** to match the `// TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...)` markers left by Phases 3 and 4 — removing those markers is a literal "replace comment with call".

---

## Task 5.2 — `PlayerSettingsPage`

### 5.2a — `Pages/PlayerSettings/PlayerSettingsPage.cs` (new)

```csharp
using System;
using System.Collections.Generic;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Screen.NewMenu.SettingsSystem;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.PlayerSettings;

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
```

Decisions baked in:

- **Render the Player category via `SettingContainer`, not hand-rolled fields.** `_draftSettings.GetVisibleSettings("Player")` returns nick (`SETTING_MENU__NICK`), color (`SETTING_MENU__COLOR`), uid (`SETTING_MENU__PLAYER_UID`), autosave (`SETTING_MENU__AUTOSAVE`) as `Setting` records bound to the draft. `new SettingContainer(setting)` builds each editor with the project's existing styling and reads edits back via the configurators. Zero new editor code; identical look to `SettingsPage`; Phase 7's category pages reuse the same pipeline. **`PlayerSettingsAcknowledged` is `[Hide]`, so it never appears in the UI** — set it on the draft directly in `OnSave`.
- **Showing uid + autosave on the gate is intentional, not a bug.** The general plan §5.1 says "ник + цвет" (minimum). Rendering the full Player category is DRYer than filtering two accessors and keeps the gate and the future Phase-7 Player category visually identical. If you want the gate to show nick + color only, filter the list: `_settings = _draftSettings.GetVisibleSettings("Player").Where(s => s.Member.Member.Name is "PlayerName" or "PlayerColor").ToList();` (needs `using System.Linq;`). Both are acceptable; default to the full category.
- **Draft/preserved mirrors `SettingsPage` exactly.** Both load `Services.MenuGameSettings.GetSettings()` at `_Ready`. Save: `SetVisibleSettings` → flip the flag → `ApplyAndSaveSettings` (runtime + persist) → continuation. Cancel: re-`ApplyAndSaveSettings(_preservedSettings)` to undo runtime side-effects of edits (e.g. a color change applies live via the configurator's `ColorChanged` handler — actually it does not, because edits write to `setting.Value`, not to live settings, until `ApplyAndSaveSettings`; but re-applying preserved is still correct and harmless), then `GoBack` with no continuation.
- **`OnSave` clears `_continuation` before invoking** so a second invocation (not currently possible, but defensive) does not double-fire. The continuation starts the game → the menu is freed by `ChangeStoredNode`; no `GoBack` after.
- **`SetContinuation` is the Phase-7 reuse seam.** When this page is later opened from the Settings hub (Phase 7) without a gate, `_continuation` is null and Save falls through to `GoBack`. Phase 7 may instead route Player-category editing through a generic `SettingsCategoryPage`; this page can stay as the gate-specialized version (the general plan's "Допущения" explicitly permits splitting gate vs. category into two classes). Leave that call to Phase 7.
- **No `Validate()` call.** `SettingsPage` does not call it either. `MenuGameSettings.Validate()` only null-coalesces the three nullable strings; the configurators cannot set them to null. Skip it for parity.

### 5.2b — `Pages/PlayerSettings/PlayerSettingsPage.tscn` (new)

Copy `Pages/Settings/SettingsPage.tscn` as the starting point, then: reattach the script to `PlayerSettingsPage.cs`, rename the root node to `PlayerSettingsPage`, change the Title Label `text` to `PLAYER_SETTINGS_MENU__TITLE`. Structure (node-for-node identical to `SettingsPage.tscn`):

```
PlayerSettingsPage (Control, full-rect, script=PlayerSettingsPage.cs)
└─ MarginContainer (margin 20)
   └─ PanelContainer (custom_minimum_size = Vector2(850, 0), size_flags_horizontal = 4)
      └─ MarginContainer (margin 20)
         └─ VBoxContainer
            ├─ Title Label  text="PLAYER_SETTINGS_MENU__TITLE"  (label_settings font_size 32, horizontal_alignment = 1)
            ├─ ScrollContainer (size_flags_vertical = 3)
            │   └─ MarginContainer (margin 10, size_flags_horizontal = 3, size_flags_vertical = 3)
            │      └─ SettingsContainer (VBoxContainer)
            └─ MarginContainer
               └─ HBoxContainer (size_flags_horizontal = 4, size_flags_vertical = 4)
                  ├─ SaveButton    text="GENERIC_MENU__SAVE_BUTTON"    (custom_minimum_size = Vector2(200, 50))
                  └─ CancelButton  text="GENERIC_MENU__CANCEL_BUTTON"  (custom_minimum_size = Vector2(200, 50))
```

Node names must match the `[Child]` properties in 5.2a **exactly**: `SettingsContainer`, `SaveButton`, `CancelButton`.

Simplest path: **duplicate `SettingsPage.tscn` in the Godot editor (FileSystem dock → right-click → Duplicate), move it into `Pages/PlayerSettings/`, rename to `PlayerSettingsPage.tscn`, reattach the script to `PlayerSettingsPage.cs`, rename the root node to `PlayerSettingsPage`, edit the Title Label text, save.** This preserves all the `MarginContainer`/`PanelContainer` sizing that the existing settings screen already tunes.

> **Optional welcome/intro line.** The spec (#16) frames this as a first-run acknowledgement. If you want a one-line instruction above the settings (e.g. "Set up your player before playing"), add a `Label` between the Title and the `ScrollContainer` with `text="PLAYER_SETTINGS_MENU__INTRO"` and add that key in Task 5.5. Default plan omits it (the title alone is clear enough); include it only if you add the key.

---

## Task 5.3 — `PagesProvider`: scene slot + `PreparePlayerSettingsPage`

### 5.3a — `Scenes/Screen/NewMenu/MainMenu/PagesProvider.cs` (edit)

Add the export field (alongside the other `[Export] [NotNull] PackedScene` lines) and the prepare helper (alongside `PrepareMessagePage`):

```csharp
[Export] [NotNull] public PackedScene PlayerSettingsPageScene { get; private set; }
```

```csharp
public Pages.PlayerSettings.PlayerSettingsPage PreparePlayerSettingsPage(Action continuation)
{
    var page = PlayerSettingsPageScene.Instantiate<Pages.PlayerSettings.PlayerSettingsPage>().WithAvailablePages(this);
    page.SetContinuation(continuation);
    return page;
}
```

`PreparePage(PackedScene)` is generic and would also work, but a dedicated helper is needed to inject the continuation (mirrors why `PrepareMessagePage` exists alongside the generic `PreparePage`). `WithAvailablePages(this)` is the `MainMenuPageExtensions` helper that calls `SetPagesProvider`.

### 5.3b — `MainMenu.tscn` (edit) — assign the new scene on the `PagesProvider` node

1. Add one new `[ext_resource]` entry for `Pages/PlayerList/PlayerSettingsPage.tscn`.
2. On the `PagesProvider` node, add `PlayerSettingsPageScene = ExtResource("<new id>")`.
3. `load_steps` at the top of `MainMenu.tscn` increases by 1 (one more ext_resource).

Easiest done in the Godot editor: open `MainMenu.tscn`, select the `PagesProvider` node, drag `PlayerSettingsPage.tscn` into the new `PlayerSettingsPageScene` export slot, save. The editor rewrites uid/path/id/load_steps correctly. Hand-editing ext-resources is fragile — prefer the editor (Phase 3 and Phase 4 implementers did this in the editor).

> **Blocking runtime check.** `PlayerSettingsPageScene` is `[NotNull]`. An empty slot passes `dotnet build` but throws on launch (`PagesProvider._Ready` → `Di.Process` → `NotNullCheck`). After saving in the editor, confirm the slot is filled before the verify step.

---

## Task 5.4 — Wrap the four game-start call sites

For each site: compute inputs first, then wrap **only the final `Services.MainScene.*(...)` call** in `TryStartGame(() => ...)`. Keep any input validation (e.g. "no save selected", "empty host") **outside** the gate — do not open the gate if the user has not satisfied the form's preconditions. Remove the `// TODO (#16 gate, Phase 5)` marker where present.

### 5.4a — `Pages/Singleplayer/SingleplayerPage.cs` (edit) — `OnStart`

This file has **no marker** (Phase 3 gap), but it is in scope (general plan §5.3 + brain). Wrap the start call.

Before:
```csharp
private void OnStart()
{
    string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
        ? Services.SaveLoad.GenNewSaveFileName()
        : SaveNameLineEdit.Text.Trim();
    Services.MainScene.StartSingleplayerGame(saveFileName);
}
```
After:
```csharp
private void OnStart()
{
    string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
        ? Services.SaveLoad.GenNewSaveFileName()
        : SaveNameLineEdit.Text.Trim();
    TryStartGame(() => Services.MainScene.StartSingleplayerGame(saveFileName));
}
```

`saveFileName` is captured by the closure; computing it before the gate is fine (pure). No validation to preserve here.

### 5.4b — `Pages/CreateNewServer/CreateNewServerPage.cs` (edit) — `ParseAndStartServer`

Before (with the Phase-3 marker):
```csharp
private void ParseAndStartServer()
{
    // TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...).
    int port = (int) PortSpinBox.Value;
    string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
        ? Services.SaveLoad.GenNewSaveFileName()
        : SaveNameLineEdit.Text.Trim();
    bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
    Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated);
}
```
After (marker removed; final call wrapped):
```csharp
private void ParseAndStartServer()
{
    int port = (int) PortSpinBox.Value;
    string saveFileName = String.IsNullOrWhiteSpace(SaveNameLineEdit.Text)
        ? Services.SaveLoad.GenNewSaveFileName()
        : SaveNameLineEdit.Text.Trim();
    bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
    TryStartGame(() => Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated));
}
```

### 5.4c — `Pages/CreateSavedServer/CreateSavedServerPage.cs` (edit) — `OnCreate`

**The marker sits ABOVE the validation guard** (`if (String.IsNullOrWhiteSpace(_selectedSaveFileName))`). Keep the validation outside the gate — open the error message page if no save is selected; only wrap the final start call.

Before:
```csharp
private void OnCreate()
{
    // TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...).
    if (String.IsNullOrWhiteSpace(_selectedSaveFileName))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("CREATE_SAVED_SERVER_MENU__NO_SAVE_SELECTED_ERROR")));
        return;
    }

    int port = (int) PortSpinBox.Value;
    bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
    Services.MainScene.HostMultiplayerGameAsClient(_selectedSaveFileName, port, isDedicated);
}
```
After:
```csharp
private void OnCreate()
{
    if (String.IsNullOrWhiteSpace(_selectedSaveFileName))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("CREATE_SAVED_SERVER_MENU__NO_SAVE_SELECTED_ERROR")));
        return;
    }

    int port = (int) PortSpinBox.Value;
    bool isDedicated = IsDedicatedCheckButton.ButtonPressed;
    TryStartGame(() => Services.MainScene.HostMultiplayerGameAsClient(_selectedSaveFileName, port, isDedicated));
}
```

### 5.4d — `Pages/ServerList/ServerListPage.cs` (edit) — `OnConnectDirect`

Parse/validate host:port first; the **known-servers auto-add moves INSIDE the continuation** so a cancelled gate does not pollute the list. Wrap the final connect (+ auto-add) in `TryStartGame`.

Before (with the Phase-4 marker):
```csharp
private void OnConnectDirect()
{
    // TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...).
    string raw = DirectHostLineEdit.Text?.Trim();
    if (String.IsNullOrWhiteSpace(raw))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
        return;
    }

    string host;
    int? port = null;
    int colon = raw.LastIndexOf(':');
    if (colon > 0 && int.TryParse(raw[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPort))
    {
        host = raw[..colon].Trim();
        port = parsedPort;
    }
    else
    {
        host = raw;
    }

    if (String.IsNullOrWhiteSpace(host))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
        return;
    }

    if (!Services.KnownServers.Exists(host, port))
    {
        Services.KnownServers.Add(new KnownServer(host, port, String.Empty));
    }

    Services.MainScene.ConnectToMultiplayerGame(host, port);
}
```
After:
```csharp
private void OnConnectDirect()
{
    string raw = DirectHostLineEdit.Text?.Trim();
    if (String.IsNullOrWhiteSpace(raw))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
        return;
    }

    string host;
    int? port = null;
    int colon = raw.LastIndexOf(':');
    if (colon > 0 && int.TryParse(raw[(colon + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedPort))
    {
        host = raw[..colon].Trim();
        port = parsedPort;
    }
    else
    {
        host = raw;
    }

    if (String.IsNullOrWhiteSpace(host))
    {
        GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
        return;
    }

    // Gate first; auto-add to known servers only when the game actually starts
    // (a cancelled gate must not mutate the list).
    TryStartGame(() =>
    {
        if (!Services.KnownServers.Exists(host, port))
        {
            Services.KnownServers.Add(new KnownServer(host, port, String.Empty));
        }
        Services.MainScene.ConnectToMultiplayerGame(host, port);
    });
}
```

Decision baked in: **auto-add moves inside the continuation.** The general plan says "Connect: ... + автодобавление в known если нового". Phase 4 put it before the service call. With the gate, if the user cancels the PlayerSettings page we must not have added the server — so both the auto-add and the connect move into the `TryStartGame` closure. The `host`/`port` locals are captured by the closure.

### 5.4e — Confirm zero markers remain

After 5.4b–5.4d, grep:
```bash
grep -rn "#16 gate\|TODO (#16" --include=*.cs --include=*.tscn .
```
Expect: **zero hits.** (5.4a had no marker to begin with.)

---

## Task 5.5 — Translation keys

Add **every** key below to **all three** files: `Assets/Locales/messages.pot` (`msgstr` empty), `Assets/Locales/en.po`, `Assets/Locales/ru.po`. Follow the existing format (blank line between entries). Place near the existing `SETTING_MENU__` block.

| msgid | en msgstr | ru msgstr |
|---|---|---|
| `PLAYER_SETTINGS_MENU__TITLE` | `Player Settings` | `Настройки игрока` |
| `PLAYER_SETTINGS_MENU__INTRO` | `Set up your player before playing.` | `Настройте игрока перед началом.` |

Notes:
- `GENERIC_MENU__SAVE_BUTTON` and `GENERIC_MENU__CANCEL_BUTTON` already exist — reuse, do not re-add.
- `SETTING_MENU__NICK` / `SETTING_MENU__COLOR` / `SETTING_MENU__PLAYER_UID` / `SETTING_MENU__AUTOSAVE` and their `*_HINT` keys already exist (Phase 1) — the `SettingContainer` pipeline reads them via `[Name]`/`[Hint]`. Do **not** re-add.
- `PLAYER_SETTINGS_MENU__INTRO` is only needed if you added the optional intro `Label` in 5.2b. If you skipped it, add only `PLAYER_SETTINGS_MENU__TITLE`.

---

## Task 5.6 — Verify

1. Confirm the new files exist:
   - `Pages/PlayerSettings/PlayerSettingsPage.cs` + `.tscn`
2. Confirm `MainMenuPage.cs` has `TryStartGame` (with `using System;`), and `PagesProvider.cs` has `PlayerSettingsPageScene` + `PreparePlayerSettingsPage`.
3. Confirm all four start handlers call `TryStartGame(() => Services.MainScene.*(...))` and that the three `// TODO (#16 gate, Phase 5)` markers are gone (grep in 5.4e returns zero hits).
4. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors.** Warnings acceptable but read them — a `CS0103` (name not found) usually means a `[Child]` name doesn't match its node name in the `.tscn`, or a missing `using`.
5. **Open `MainMenu.tscn` in the Godot editor** and confirm the `PagesProvider` node has `PlayerSettingsPageScene` assigned (no empty export slot — an empty `[NotNull]` export throws at startup). This is blocking: a missed assignment passes `dotnet build` but crashes on launch.
6. **Recommended editor run** (the implementer is not strictly required to play through, but this phase's whole point is runtime behaviour): with `game-settings.json` absent or with `"PlayerSettingsAcknowledged": false`:
   - From MainPage → Singleplayer → Start → the **PlayerSettingsPage** appears (not the game). Edit nick/color → Save → the singleplayer game starts. On subsequent restarts (flag now `true`), Start goes straight into the game with no gate.
   - Repeat for Multiplayer → Create new / Create from save / Connect — each first run opens the gate, Save proceeds.
   - Cancel on the gate returns to the previous form with no game start and no change to `known-servers.json` (verify the ServerList auto-add did not fire on cancel).
   - Confirm `user://game-settings.json` now has `"PlayerSettingsAcknowledged": true` after a Save.

---

## Out of scope for Phase 5 (do NOT do)

- **Custom `ColorPickerPanel`** — Phase 6. The gate uses the stock `ColorPickerButton` that the existing `Color` configurator renders inside `SettingContainer`. Phase 6 replaces the `Color` configurator project-wide, which upgrades this page automatically.
- **`SettingsHubPage` / generic `SettingsCategoryPage` / 5 category pages / `ConfirmDialogPage`** — Phase 7. `MainPage.SettingsButton` keeps pointing at the existing flat `SettingsPage`. Phase 7 will decide whether `PlayerSettingsPage` stays as the gate-specialized page or folds into a generic category page — leave that decision to Phase 7.
- **Removing the now-unused flat `SettingsPage`** — Phase 7 deletes it.
- **`AutoScale` (#137)** — not in this epic. `InterfaceSize` already exists from Phase 1; real scaling is separate.
- **Changing the gate field's `[Hide]` / `[Category("Player")]` attributes** — Phase 1 set them deliberately (hidden so it never renders; categorised so it groups with Player). Do not un-hide it.
- **Editing `GameSettings`/`MenuGameSettings`/`MenuGameSettingsService`** — Phase 1 already wired `PlayerSettingsAcknowledged` end-to-end. This phase only reads and flips it.
- **Changing `MainMenu.tscn`'s 3D background / `PageContainer` wiring** — untouched.

---

## Gotchas recap

- **`TryStartGame` lives on `MainMenuPage`, not a static helper.** `GoNext` is `protected` on `Page`; a static utility cannot reach it. `MainMenuPage` is the common base of all four start pages and already holds `PagesProvider`.
- **Game starts free the menu.** Every `Services.MainScene.*` start method calls `_mainSceneContainer.ChangeStoredNode(game)`, replacing the MainMenu tree. The continuation just invokes the service — no `GoBack`, no cleanup, after starting.
- **The gate field is `[Hide]`.** It never appears in `GetVisibleSettings("Player")`. Set it on the `_draftSettings` object directly in `OnSave` (`_draftSettings.PlayerSettingsAcknowledged = true;`), then `ApplyAndSaveSettings`.
- **Keep form validation OUTSIDE the gate.** `CreateSavedServerPage.OnCreate` (no-save-selected) and `ServerListPage.OnConnectDirect` (empty/invalid host) validate before `TryStartGame`. Do not open the gate if the form is invalid.
- **Move `ServerListPage`'s known-servers auto-add INSIDE the `TryStartGame` closure.** A cancelled gate must not mutate `known-servers.json`. `host`/`port` are captured by the closure.
- **`SingleplayerPage.OnStart` had no `// TODO` marker** (Phase 3 gap) but is in scope — wrap it anyway. After this phase the grep for `#16 gate` / `TODO (#16` must return zero hits.
- **Color editor = stock `ColorPickerButton` via the `Color` configurator.** Do not build a custom picker (Phase 6). Rendering the Player category through `SettingContainer` gives you a working picker for free.
- **Hand-editing `MainMenu.tscn` ext-resources is fragile.** Assign `PlayerSettingsPageScene` in the Godot editor; it manages uid/path/load_steps. A missed `[NotNull]` slot passes `dotnet build` but crashes on launch — verify in the editor.
- **Draft/preserved pattern is mandatory in `PlayerSettingsPage`.** Load both from `Services.MenuGameSettings.GetSettings()` at `_Ready`; Save applies draft + flips flag + persists + continuation; Cancel re-applies preserved + returns. Mirrors `SettingsPage` exactly.
- **`PlayerSettingsPage` is a form, not a hub.** Use the PanelContainer shell copied from `SettingsPage.tscn`, not the MainPage neon-button template.
