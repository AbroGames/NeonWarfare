# Phase 3 — Multiplayer Hub + Server Forms (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 3 of 8. **Prerequisite for:** Phase 4 (`ServerListPage` repoints `MultiplayerPage.Connect`). **Blocked by:** nothing (Phases 1/2 need not be merged, only built). **Blocks nothing hard** — Phase 5 wraps the start handlers created here in the first-run gate.

This phase adds the **Multiplayer hub page**, restructures **MainPage** to expose it, splits the current single `HostPage` form into **CreateNewServerPage** (new-game form) and **CreateSavedServerPage** (form + searchable save-file picker), and deletes the now-superseded **HostPage**. `ConnectPage` is **kept** (its replacement `ServerListPage` is Phase 4) and is reused as the temporary target of the hub's Connect button.

## Goal (done = all of)

1. `MultiplayerPage` exists under `Pages/Multiplayer/` with 4 buttons (Create New / Create from Save / Connect / Back) styled like MainPage's neon buttons, each navigating via `GoNext(PagesProvider.PreparePage(...))`.
2. `MainPage` exposes a single `MultiplayerButton` (replacing `CreateServerButton` + `ConnectToServerButton`) that opens `MultiplayerPage`. Resume/Singleplayer/Settings/Language/Quit stay.
3. `CreateNewServerPage` exists under `Pages/CreateNewServer/` with Port SpinBox (1024–65535, default 25566), SaveName LineEdit (pre-filled with `GenNewSaveFileName()`), Dedicated CheckButton, Create/Back buttons. Create calls `Services.MainScene.HostMultiplayerGameAsClient(saveFileName, port, isDedicated)` — the exact logic of `HostPage.ParseAndStartServer`.
4. `CreateSavedServerPage` exists under `Pages/CreateSavedServer/` with Port SpinBox, a searchable save-file list (LineEdit filter + ScrollContainer + VBox of buttons), Dedicated CheckButton, Create/Back buttons. Create calls `HostMultiplayerGameAsClient(selectedSaveFileName, port, isDedicated)`; if no save selected, shows an inline error via `PrepareMessagePage`.
5. `PagesProvider` exports `MultiplayerPageScene`, `CreateNewServerPageScene`, `CreateSavedServerPageScene` (all `[Export][NotNull] PackedScene`), and the old `CreateServerPageScene` export is removed. `ConnectionPageScene` stays (still used by the hub's Connect button until Phase 4).
6. `HostPage.cs` + `HostPage.tscn` are **deleted** (logic fully ported to `CreateNewServerPage`; no remaining references).
7. New locale keys present in `messages.pot`, `en.po`, `ru.po`.
8. `dotnet build` succeeds with **0 errors**.

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Page subclass recipe.** Every page lives in `Scenes/Screen/NewMenu/MainMenu/Pages/<Name>/<Name>Page.cs` + `<Name>Page.tscn`, is `partial class <Name>Page : MainMenuPage`, injects nodes via `[Child]` (KludgeBox, `KludgeBox.DI.Requests.ChildInjection`), calls `Di.Process(this)` in `_Ready()` (resolves to `Services.Di`), and navigates via the `protected Action<IPage> GoNext` / `protected Action GoBack` inherited from `Page` (`Scenes/Screen/NewMenu/PagesSystem/Page.cs`). `PagesProvider` (a `MainMenuPage` exposes it via `protected PagesProvider PagesProvider`) has `MainMenuPage PreparePage(PackedScene)` — generic, no new overload needed.
- **Hub = MainPage-style buttons; forms = HostPage-style shell.** Two distinct visual templates:
  - **Neon nav button** (MainPage / the new hub): `theme_override_styles/normal = StyleBoxEmpty` (with `content_margin_left = 48.0`) + `theme_override_styles/hover = StyleBoxTexture` over a cyan `GradientTexture1D`; font `Play-Bold.ttf` size 18; `custom_minimum_size = Vector2(0, 35)`; `alignment = 0`. Copy the block verbatim from any button in `Pages/Main/MainPage.tscn`.
  - **Form shell** (the two server pages): root `Control` full-rect → `MarginContainer` (margin 20) → `PanelContainer` (`custom_minimum_size = Vector2(850, 0)`, centered) → inner `MarginContainer` (margin 20) → `VBoxContainer` (Title Label font_size 32 centered, Separator, InputsContainer VBox h-center, Separator, Buttons HBox h-center). This is the exact skeleton of `Pages/Host/HostPage.tscn` and `Pages/Connect/ConnectPage.tscn` — copy one and edit.
- **`[Child]` resolution is by name.** The injected property name must match the node name in the `.tscn` (`[Child] public Button CreateButton` → a node named `CreateButton`). This is how every existing page works.
- **Start-game service signatures** (`Scripts/Service/MainSceneService.cs`, exposed as `Services.MainScene`):
  - `HostMultiplayerGameAsClient(string saveFileName, int? port = null, bool createDedicatedServerProcess = false)` — `port` is `int?` (an `int` arg promotes implicitly); the bool is the dedicated flag.
  - `ConnectToMultiplayerGame(string host = null, int? port = null)`.
  - **No gate yet.** Phase 5 wraps these calls in `TryStartGame(...)`. In Phase 3 call the service directly, exactly as `HostPage` and `ConnectPage` do today. Leave a `// TODO (#16 gate, Phase 5)` comment on each start handler so Phase 5 finds them.
- **Save-file API** (`Scripts/Service/SaveLoadService.cs`, exposed as `Services.SaveLoad`):
  - `List<SaveFileInfo> GetAllSaveFiles()` — `SaveFileInfo` is `readonly record struct(string FileName, ulong ModifiedTime)`. Ordered by `ModifiedTime` desc.
  - `string GenNewSaveFileName()` — returns `"yyyy-MM-dd_HH-mm"` (invariant culture).
  - **Do not** hardcode the save dir or `.bin` extension; always go through `GetAllSaveFiles()`.
- **Inline error pattern.** To show an error and return to the current page, push a `MessagePage`: `GoNext(PagesProvider.PrepareMessagePage(Tr("...KEY...")))`. The user clicks OK (`MessagePage.OkButton` calls `GoBack`) and lands back on the current page. This is how `ConnectPage.ParseAndConnectToServer` reports a missing host. Use it in `CreateSavedServerPage` when no save is selected.
- **`PagesProvider` export naming.** Existing exports: `MainPageScene`, `SettingsPageScene`, `ConnectionPageScene`, `CreateServerPageScene` (= HostPage!), `MessagePageScene`, `LanguageSelectionPageScene`, `SingleplayerPage` (this last one breaks the `*Scene` convention — leave it). `MainMenu.tscn` wires each export to an `ExtResource` on the `PagesProvider` node. New exports must be wired there too, or `[NotNull]` validation throws at startup.
- **`HostPage` is referenced only by** `PagesProvider.CreateServerPageScene` and `MainPage.CreateServerButton` (both removed in this phase). `ConnectPage` is referenced only by `PagesProvider.ConnectionPageScene` and (after this phase) the new hub's Connect button. Verified — no other references exist.
- **Locale convention.** `SECTION__KEY`, three files: `Assets/Locales/messages.pot` (empty `msgstr`), `en.po`, `ru.po`. `Tr()` is identity for unknown keys, so a missing translation renders the key verbatim — but all keys must still be added to all three files.

---

## Task 3.1 — `MultiplayerPage` (hub)

### 3.1a — `Pages/Multiplayer/MultiplayerPage.cs` (new)

```csharp
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.Multiplayer;

public partial class MultiplayerPage : MainMenuPage
{
    [Child] public Button CreateNewServerButton { get; private set; }
    [Child] public Button CreateFromSaveButton { get; private set; }
    [Child] public Button ConnectButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);

        CreateNewServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.CreateNewServerPageScene));
        CreateFromSaveButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.CreateSavedServerPageScene));
        // Connect targets the existing ConnectPage for now; Phase 4 repoints this to ServerListPage
        // and deletes ConnectPage. Do not "fix" this until Phase 4.
        ConnectButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ConnectionPageScene));
        BackButton.Pressed += () => GoBack();
    }
}
```

### 3.1b — `Pages/Multiplayer/MultiplayerPage.tscn` (new)

Use the **MainPage neon-button template**, not the form shell. Root is a full-rect `Control` with the script attached. Structure:

```
MultiplayerPage (Control, full-rect, script=MultiplayerPage.cs)
└─ MarginContainer (margins 110/25/25/25 — copy MainPage's left/top/right/bottom)
   └─ VBoxContainer (custom_minimum_size x=450)
      ├─ Title Label  text="MULTIPLAYER_MENU__TITLE"  (Play-Bold, size 32, or reuse MainPage's LabelSettings_title look)
      ├─ Separator (Label, empty, min height spacer)
      ├─ CreateNewServerButton  text="MULTIPLAYER_MENU__CREATE_NEW_BUTTON"      [neon button style block]
      ├─ CreateFromSaveButton   text="MULTIPLAYER_MENU__CREATE_FROM_SAVE_BUTTON" [neon button style block]
      ├─ ConnectButton          text="MULTIPLAYER_MENU__CONNECT_BUTTON"         [neon button style block]
      ├─ Separator (Label, empty)
      └─ BackButton             text="GENERIC_MENU__BACK_BUTTON"                [neon button style block]
```

The **neon button style block** per button (copy verbatim from `MainPage.tscn`'s `StartSingleplayerButton`):

```
custom_minimum_size = Vector2(0, 35)
theme_override_colors/font_color = Color(1, 1, 1, 1)
theme_override_colors/font_hover_color = Color(0, 0, 0, 1)
theme_override_fonts/font = ExtResource("<Play-Bold.ttf ext resource>")
theme_override_font_sizes/font_size = 18
theme_override_styles/normal = SubResource("StyleBoxEmpty_normal")
theme_override_styles/hover = SubResource("StyleBoxTexture_hover")
alignment = 0
```

Define `StyleBoxEmpty_normal` (`content_margin_left = 48.0`), `Gradient_hover`, `GradientTexture1D_hover`, `StyleBoxTexture_hover` as sub-resources exactly as in `MainPage.tscn`. Add the `Play-Bold.ttf` ext-resource. Simplest path: **open `MainPage.tscn`, Save As… into the new folder, then strip to the 4 buttons + title and reattach the new script.** Easier in the Godot editor than hand-authoring the `.tscn`.

---

## Task 3.2 — Restructure `MainPage`

### 3.2a — `Pages/Main/MainPage.cs` (edit)

Replace the two `[Child]` fields and their handlers with one `MultiplayerButton`.

Remove:
```csharp
[Child] public Button CreateServerButton { get; private set; }
[Child] public Button ConnectToServerButton { get; private set; }
```
Add:
```csharp
[Child] public Button MultiplayerButton { get; private set; }
```

In `_Ready()`, remove these two lines:
```csharp
CreateServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.CreateServerPageScene));
ConnectToServerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ConnectionPageScene));
```
Add in their place:
```csharp
MultiplayerButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.MultiplayerPageScene));
```

Leave `ResumeButton`, `StartSingleplayerButton`, `SettingsButton`, `LanguageButton`, `QuitButton` and their handlers untouched.

### 3.2b — `Pages/Main/MainPage.tscn` (edit)

Delete the `CreateServerButton` and `ConnectToServerButton` nodes (currently siblings of `StartSingleplayerButton` under `ScreenSplit/ControlsContainer`). Add one `MultiplayerButton` node in their place (same parent, same neon style block), with `text = "MAIN_MENU__MULTIPLAYER_BUTTON"`. Keep it between `StartSingleplayerButton` and the existing `Separator`.

The `StartSingleplayerButton` stays as-is (singleplayer remains a top-level entry per the spec tree). Do not rename it.

---

## Task 3.3 — `CreateNewServerPage` (port of HostPage form)

### 3.3a — `Pages/CreateNewServer/CreateNewServerPage.cs` (new)

This is `HostPage` with one tweak: `SaveName` is a `LineEdit` (not `TextEdit`) and is pre-filled with `GenNewSaveFileName()`. Port `HostPage.ParseAndStartServer` verbatim.

```csharp
using System;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.CreateNewServer;

public partial class CreateNewServerPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public LineEdit SaveNameLineEdit { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    public override void _Ready()
    {
        Di.Process(this);
        CreateServerButton.Pressed += ParseAndStartServer;
        CancelButton.Pressed += () => GoBack();
        PortSpinBox.Value = Consts.DefaultPort;
        SaveNameLineEdit.Text = Services.SaveLoad.GenNewSaveFileName();
        IsDedicatedCheckButton.ButtonPressed = false;
    }

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
}
```

`Consts.DefaultPort` is `25566` (`Scripts/Consts.cs`).

### 3.3b — `Pages/CreateNewServer/CreateNewServerPage.tscn` (new)

Copy `Pages/Host/HostPage.tscn` as the starting point and make these edits:

- Reattach script → `CreateNewServerPage.cs`.
- Root node name: `CreateNewServerPage`.
- **`PortSpinBox`**: add `min_value = 1024` (keep `max_value = 65535`, `value = 25566`). HostPage's SpinBox had no lower bound; the spec (3.3) requires 1024–65535.
- **`SaveNameTextEdit`** → rename to `SaveNameLineEdit` and change its type from `TextEdit` to `LineEdit` (so `[Child] public LineEdit SaveNameLineEdit` resolves). Keep `custom_minimum_size = Vector2(200, 0)` and expand flags.
- Retitle: Title Label `text = "CREATE_SERVER_MENU__TITLE"`.
- Relabel inputs: Port label `CREATE_SERVER_MENU__PORT`, SaveName label `CREATE_SERVER_MENU__SAVE_NAME`, dedicated `CREATE_SERVER_MENU__DEDICATED`, create button `CREATE_SERVER_MENU__CREATE_BUTTON`, cancel `GENERIC_MENU__CANCEL_BUTTON`.
- Keep the PanelContainer (min width 850), MarginContainers (20), VBox, Separators, Buttons HBox layout identical to HostPage.

---

## Task 3.4 — `CreateSavedServerPage` (form + searchable save picker)

### 3.4a — `Pages/CreateSavedServer/CreateSavedServerPage.cs` (new)

Combines: HostPage's Port + Dedicated + Create/Cancel, with SingleplayerPage's save-list pattern, plus a live search filter.

```csharp
using System;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.CreateSavedServer;

public partial class CreateSavedServerPage : MainMenuPage
{
    [Child] public SpinBox PortSpinBox { get; private set; }
    [Child] public LineEdit SearchLineEdit { get; private set; }
    [Child] public VBoxContainer SavesListContainer { get; private set; }
    [Child] public CheckButton IsDedicatedCheckButton { get; private set; }
    [Child] public Button CreateServerButton { get; private set; }
    [Child] public Button CancelButton { get; private set; }

    private string _selectedSaveFileName;

    public override void _Ready()
    {
        Di.Process(this);

        CreateServerButton.Pressed += OnCreate;
        CancelButton.Pressed += () => GoBack();
        SearchLineEdit.TextChanged += OnSearchChanged;

        PortSpinBox.Value = Consts.DefaultPort;
        IsDedicatedCheckButton.ButtonPressed = false;
        SearchLineEdit.Text = String.Empty;

        _selectedSaveFileName = Services.SaveLoad.GetAllSaveFiles().FirstOrDefault().FileName ?? String.Empty;
        PopulateSavesList(SearchLineEdit.Text);
    }

    private void PopulateSavesList(string filter)
    {
        // Clear existing rows (skip any non-Button children if added later; here it's all buttons).
        foreach (var child in SavesListContainer.GetChildren())
        {
            child.QueueFree();
        }

        var saves = Services.SaveLoad.GetAllSaveFiles();
        string filterLower = (filter ?? String.Empty).Trim().ToLowerInvariant();

        foreach (var save in saves)
        {
            if (!String.IsNullOrEmpty(filterLower)
                && !save.FileName.ToLowerInvariant().Contains(filterLower))
            {
                continue;
            }

            var button = new Button();
            button.Text = $"{save.FileName} ({DateTimeOffset.FromUnixTimeSeconds((long)save.ModifiedTime).ToLocalTime():yyyy-MM-dd HH:mm})";
            button.Pressed += () =>
            {
                _selectedSaveFileName = save.FileName;
            };
            // Visually mark the current selection.
            if (save.FileName == _selectedSaveFileName)
            {
                button.ButtonPressed = true;
            }
            SavesListContainer.AddChild(button);
        }
    }

    private void OnSearchChanged(string newText)
    {
        // Re-populate on every keystroke. Simple and robust; save counts are small.
        PopulateSavesList(newText);
    }

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
}
```

Decisions baked in:

- **Filter = re-populate on every `TextChanged`.** Matches the general plan ("перепопуляция списка при каждом изменении, case-insensitive contains по FileName"). Save counts are tiny (a user's local saves); the simpler full re-render beats diffing.
- **Selection survives filtering** by name: `_selectedSaveFileName` is remembered independent of which rows are visible; when the selected save reappears in the list it is shown pressed.
- **No save selected → inline error** via `PrepareMessagePage` (the `ConnectPage` pattern), not a disabled button. The Create button stays always enabled; the error teaches the user to pick a row.
- **Start handler calls the service directly** (no gate) — gate lands in Phase 5.
- **`SearchLineEdit.TextChanged` signature** is `EventHandler<string>` (Godot emits the new text) — bind directly with `+=`.

### 3.4b — `Pages/CreateSavedServer/CreateSavedServerPage.tscn` (new)

Use the HostPage form shell for Port + Dedicated + buttons, and the SingleplayerPage list subtree for the picker. Structure:

```
CreateSavedServerPage (Control, full-rect, script=CreateSavedServerPage.cs)
└─ MarginContainer (margin 20)
   └─ PanelContainer (custom_minimum_size = Vector2(850, 0), h/v centered)
      └─ MarginContainer (margin 20)
         └─ VBoxContainer
            ├─ Title Label  text="CREATE_SAVED_SERVER_MENU__TITLE"  (font_size 32, centered)
            ├─ Separator (Control, min 0,20)
            ├─ PortContainer (HBox, h-center)
            │   ├─ PortLabel  text="CREATE_SAVED_SERVER_MENU__PORT"
            │   └─ PortSpinBox  (min_value=1024, max_value=65535, value=25566)
            ├─ SearchLineEdit  (placeholder_text="CREATE_SAVED_SERVER_MENU__SEARCH_PLACEHOLDER",
            │                   custom_minimum_size = Vector2(300, 0), h-expand)
            ├─ PanelContainer  (v-expand)              ← list frame (copy from SingleplayerPage)
            │   └─ MarginContainer (margin 10)
            │      └─ ScrollContainer (v-expand)
            │         └─ MarginContainer (margin 10, h/v expand)
            │            └─ SavesListContainer (VBoxContainer, custom_minimum_size = Vector2(300, 0), h-center)
            ├─ IsDedicatedCheckButton  text="CREATE_SAVED_SERVER_MENU__DEDICATED"
            ├─ Separator (Control, min 0,20)
            └─ Buttons (HBox, h-center)
               ├─ CreateServerButton  text="CREATE_SAVED_SERVER_MENU__CREATE_BUTTON"  (min 200,50)
               └─ CancelButton        text="GENERIC_MENU__CANCEL_BUTTON"              (min 200,50)
```

The list frame (`PanelContainer > MarginContainer > ScrollContainer > MarginContainer > SavesListContainer`) is copied node-for-node from `Pages/Singleplayer/SingleplayerPage.tscn`'s load-tab subtree. Simplest path: copy `HostPage.tscn` for the outer shell, then graft the SingleplayerPage list subtree in.

Node names must match the `[Child]` properties exactly: `PortSpinBox`, `SearchLineEdit`, `SavesListContainer`, `IsDedicatedCheckButton`, `CreateServerButton`, `CancelButton`.

---

## Task 3.5 — `SingleplayerPage`

**No code change in Phase 3.** The page stays as-is; its start handlers get wrapped in the first-run gate (#16) in **Phase 5**. This task is recorded here only because the general plan lists it under Phase 3 — the actual edit is deferred. Do not touch `SingleplayerPage.cs` / `.tscn` now.

(MainPage's `StartSingleplayerButton` already navigates to `SingleplayerPage` and remains in place after Task 3.2; nothing to rewire.)

---

## Task 3.6 — `PagesProvider` exports + delete `HostPage`

### 3.6a — `PagesProvider.cs` (edit)

Add three exports. Remove the `CreateServerPageScene` export (HostPage is being deleted). Keep `ConnectionPageScene` (the hub's Connect button still uses it until Phase 4).

```csharp
[Export] [NotNull] public PackedScene MainPageScene { get; private set; }
[Export] [NotNull] public PackedScene SettingsPageScene { get; private set; }
[Export] [NotNull] public PackedScene ConnectionPageScene { get; private set; }
[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
[Export] [NotNull] public PackedScene LanguageSelectionPageScene { get; private set; }
[Export] [NotNull] public PackedScene SingleplayerPage { get; private set; }
[Export] [NotNull] public PackedScene MultiplayerPageScene { get; private set; }
[Export] [NotNull] public PackedScene CreateNewServerPageScene { get; private set; }
[Export] [NotNull] public PackedScene CreateSavedServerPageScene { get; private set; }
```

(Removed: `CreateServerPageScene`. Added: the last three.) `PreparePage(PackedScene)` needs no change — it is generic over `MainMenuPage`.

### 3.6b — `MainMenu.tscn` (edit) — rewire the `PagesProvider` node

On the `PagesProvider` node (currently lines ~30–38 of `MainMenu.tscn`):

1. **Remove** the `CreateServerPageScene = ExtResource("5_36rd5")` line **and** its `[ext_resource ... HostPage.tscn ...]` entry from the header.
2. **Add** three new `[ext_resource]` entries for the three new `.tscn` files, and three matching assignments on the `PagesProvider` node:
   - `MultiplayerPageScene = ExtResource("<new id>")`
   - `CreateNewServerPageScene = ExtResource("<new id>")`
   - `CreateSavedServerPageScene = ExtResource("<new id>")`
3. Bump the scene's `load_steps` count up by 2 (3 added, 1 removed → net +2).

Easiest done in the Godot editor: open `MainMenu.tscn`, select the `PagesProvider` node, drag the three new scenes into its inspector export slots, remove the old `CreateServerPageScene` assignment, save. The editor rewrites the `.tscn` correctly. Hand-editing the `.tscn` is error-prone (uid/path/id bookkeeping) — prefer the editor.

### 3.6c — Delete `HostPage`

Delete both files:
- `Scenes/Screen/NewMenu/MainMenu/Pages/Host/HostPage.cs`
- `Scenes/Screen/NewMenu/MainMenu/Pages/Host/HostPage.tscn`
- Remove the now-empty `Host/` directory.

**Pre-check before deleting** (catch any reference missed above):
```bash
grep -rn "HostPage\|Pages.Host\b" --include=*.cs --include=*.tscn .
```
Expected after Tasks 3.2a + 3.6a: **zero hits.** If any remain, do not delete — resolve the reference first.

> **Why delete HostPage when the general plan's file summary doesn't list it.** Task 3.3 ports `HostPage.ParseAndStartServer` verbatim into `CreateNewServerPage`; after 3.2a + 3.6a nothing references `HostPage`. Keeping it leaves a dead, unstyled scene that drifts from the real form. The general plan's "Удаляемые (≈4)" list is approximate ("≈") and was written before the Phase-3/Phase-4 split was finalized; deleting HostPage alongside its replacement is the self-consistent choice. `ConnectPage` is **not** deleted here — its replacement (`ServerListPage`) is Phase 4.

---

## Task 3.7 — Translation keys

Add **every** key below to **all three** files: `Assets/Locales/messages.pot` (`msgstr` empty), `Assets/Locales/en.po`, `Assets/Locales/ru.po`. Follow the existing format (blank line between entries). Group the new sections near the existing `MAIN_MENU__` / `HOST_MENU__` blocks.

| msgid | en msgstr | ru msgstr |
|---|---|---|
| `MAIN_MENU__MULTIPLAYER_BUTTON` | `Multiplayer` | `Игра по сети` |
| `MULTIPLAYER_MENU__TITLE` | `Multiplayer` | `Игра по сети` |
| `MULTIPLAYER_MENU__CREATE_NEW_BUTTON` | `Create new game` | `Создать новую игру` |
| `MULTIPLAYER_MENU__CREATE_FROM_SAVE_BUTTON` | `Create from save` | `Создать из сохранения` |
| `MULTIPLAYER_MENU__CONNECT_BUTTON` | `Connect to server` | `Подключиться к серверу` |
| `CREATE_SERVER_MENU__TITLE` | `Create Server` | `Создать сервер` |
| `CREATE_SERVER_MENU__PORT` | `Port:` | `Порт:` |
| `CREATE_SERVER_MENU__SAVE_NAME` | `Save Name:` | `Имя сохранения:` |
| `CREATE_SERVER_MENU__DEDICATED` | `Dedicated?` | `Выделенный?` |
| `CREATE_SERVER_MENU__CREATE_BUTTON` | `Create server` | `Создать сервер` |
| `CREATE_SAVED_SERVER_MENU__TITLE` | `Create Server from Save` | `Создать сервер из сохранения` |
| `CREATE_SAVED_SERVER_MENU__PORT` | `Port:` | `Порт:` |
| `CREATE_SAVED_SERVER_MENU__SEARCH_PLACEHOLDER` | `Search saves...` | `Поиск сохранений...` |
| `CREATE_SAVED_SERVER_MENU__DEDICATED` | `Dedicated?` | `Выделенный?` |
| `CREATE_SAVED_SERVER_MENU__CREATE_BUTTON` | `Create server` | `Создать сервер` |
| `CREATE_SAVED_SERVER_MENU__NO_SAVE_SELECTED_ERROR` | `Select a save file first` | `Сначала выберите сохранение` |
| `GENERIC_MENU__BACK_BUTTON` | `Back` | `Назад` |

Notes:
- `GENERIC_MENU__CANCEL_BUTTON` and `GENERIC_MENU__SAVE_BUTTON` already exist — reuse, do not re-add.
- The old `HOST_MENU__*` keys become **orphaned** once HostPage is deleted. **Leave them in place** for Phase 3 (removing them now is unrelated churn); Phase 8's locale pass removes them along with `CONNECT_MENU__*` (orphaned in Phase 4). Flagged in the plan so the implementer doesn't "clean them up" speculatively.
- Port label appears under two sections (`CREATE_SERVER_MENU__PORT`, `CREATE_SAVED_SERVER_MENU__PORT`). Duplicated by design — keeps each page's keys self-contained and matches the general plan's per-section key scheme.

---

## Task 3.8 — Verify

1. Confirm the new files exist:
   - `Pages/Multiplayer/MultiplayerPage.cs` + `.tscn`
   - `Pages/CreateNewServer/CreateNewServerPage.cs` + `.tscn`
   - `Pages/CreateSavedServer/CreateSavedServerPage.cs` + `.tscn`
2. Confirm `HostPage.cs`, `HostPage.tscn`, and the `Host/` folder are gone, and the grep in 3.6c returns zero hits.
3. Confirm `PagesProvider.cs` compiles (new exports, no `CreateServerPageScene`), and `MainPage.cs` compiles (no `CreateServerButton`/`ConnectToServerButton`, has `MultiplayerButton`).
4. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors.** Warnings acceptable but read them — a `CS0114` (hide) or `CS0103` (name not found) usually means a `[Child]` name doesn't match its node name in the `.tscn`.
5. **Open `MainMenu.tscn` in the Godot editor** and confirm the `PagesProvider` node has all three new scenes assigned (no empty export slots — empty `[NotNull]` exports throw at startup). This is blocking: a missed assignment passes `dotnet build` but crashes on launch.
6. The implementer is **not** required to play through the game in Phase 3, but a quick editor run to confirm the menu navigates `Main → Multiplayer → Create New / Create from Save / Connect → Back` and that `Create New` starts a hosted game is the recommended sanity check.

---

## Out of scope for Phase 3 (do NOT do)

- **`ServerListPage`** and deleting `ConnectPage` — Phase 4. The hub's Connect button points to the existing `ConnectPage` for now.
- **First-run gate (#16)** — Phase 5. Start handlers call the service directly; the `// TODO (#16 gate, Phase 5)` comments mark the wrap points.
- **`SettingsHubPage` / category pages** — Phase 7. `MainPage.SettingsButton` keeps pointing at the existing `SettingsPage`.
- **Custom `ColorPickerPanel`** — Phase 6.
- **Removing `HOST_MENU__*` / `CONNECT_MENU__*` locale keys** — Phase 8 cleanup. Leave orphaned keys in place.
- **`SingleplayerPage` edits** — its gating is Phase 5; no change now (see Task 3.5).
- **Renaming `SingleplayerPage` export** to `SingleplayerPageScene` to match the convention — cosmetic, not in this phase's scope.
- **Changing `MainMenu.tscn`'s 3D background / `PageContainer` wiring** — untouched.

---

## Gotchas recap

- **Hub uses the neon-button template; the two forms use the HostPage form shell.** Do not cross them. Copy from `MainPage.tscn` for the hub, from `HostPage.tscn` for the forms.
- **`[Child]` resolves by node name.** Every `[Child] public X Foo` needs a node named exactly `Foo` in the `.tscn`. A mismatch compiles but throws at `_Ready` injection time.
- **`CreateServerPageScene` in `PagesProvider` is HostPage, not a "create server" page.** Remove that export when deleting HostPage; the new `CreateNewServerPageScene` is unrelated and must not inherit the old uid/path.
- **Hand-editing `MainMenu.tscn` ext-resources is fragile.** Do the `PagesProvider` rewiring in the Godot editor; it manages uid/path/load_steps correctly.
- **`PortSpinBox` lower bound.** HostPage had none; both new forms set `min_value = 1024` per spec. Don't forget it when copying the template.
- **`SaveName` is a `LineEdit`, not `TextEdit`.** `CreateNewServerPage` intentionally diverges from `HostPage` here — match the SingleplayerPage convention and the general plan.
- **Connect button target is deliberately the old `ConnectPage`.** Phase 4 changes it. The inline comment in `MultiplayerPage._Ready` exists precisely so a Phase-4 implementer (or you, later) doesn't read it as a bug.
- **Search filter re-populates the whole list per keystroke** — intentional, keep it simple; don't try to diff rows.
- **`HostPage` deletion is in scope here** (not Phase 8) because its replacement lands in this same phase. `ConnectPage` deletion is Phase 4 because *its* replacement lands then.
- **Leave a `// TODO (#16 gate, Phase 5)` on every start handler** (`CreateNewServerPage.ParseAndStartServer`, `CreateSavedServerPage.OnCreate`). Phase 5 greps for these.
