# Phase 4 — ServerListPage + direct connect (detailed)

**Epic:** #12 (MainMenu rebuild). **Phase:** 4 of 8. **Blocked by:** Phase 3 (the hub `MultiplayerPage.ConnectButton` exists and currently points at `ConnectionPageScene`). **Prerequisite for:** Phase 5 wraps this page's `OnConnect` handler in the first-run gate (#16).

This phase adds **`ServerListPage`** (the known-servers list + add/remove + direct `host:port` connect, backed by `Services.KnownServers` from Phase 2), **repoints** the `MultiplayerPage` Connect button at it, and **deletes** the now-superseded **`ConnectPage`**.

## Goal (done = all of)

1. `ServerListPage` exists under `Pages/ServerList/` with: a scrollable list of known servers (one row per `KnownServer`, label shows `host:port` + optional `Label`), click-to-select; Add (inline host+port+label form, validates and calls `Services.KnownServers.Add`), Remove (removes the selected row via `Services.KnownServers.Remove`), a direct-connect `host:port` `LineEdit` + Connect button, Back.
2. Connecting (from a list row **or** from the direct-connect field) calls `Services.MainScene.ConnectToMultiplayerGame(host, port)` and **auto-adds** the server to known servers if it is not already known.
3. `MultiplayerPage.ConnectButton` is repointed from `ConnectionPageScene` to the new `ServerListPageScene` (Phase 3 left this as a deliberate temporary target).
4. `ConnectPage.cs` + `ConnectPage.tscn` are **deleted** (no remaining references); `ConnectionPageScene` export is removed from `PagesProvider` and rewired to `ServerListPageScene` in both `PagesProvider.cs` and `MainMenu.tscn`.
5. New locale keys present in `messages.pot`, `en.po`, `ru.po`.
6. `dotnet build` succeeds with **0 errors**.

---

## Context you must know before editing

Read these facts. Do not assume otherwise.

- **Page subclass recipe.** Every page lives in `Scenes/Screen/NewMenu/MainMenu/Pages/<Name>/<Name>Page.cs` + `<Name>Page.tscn`, is `partial class <Name>Page : MainMenuPage`, injects nodes via `[Child]` (KludgeBox, `KludgeBox.DI.Requests.ChildInjection`), calls `Di.Process(this)` in `_Ready()`, and navigates via `protected Action<IPage> GoNext` / `protected Action GoBack` inherited from `Page`. `PagesProvider.PreparePage(PackedScene)` is generic — no new overload needed.
- **Form shell template.** `ServerListPage` is a **form** (PanelContainer shell), not a hub. Copy the outer skeleton from `Pages/Connect/ConnectPage.tscn` or `Pages/CreateSavedServer/CreateSavedServerPage.tscn`: root `Control` full-rect → `MarginContainer` (margin 20) → `PanelContainer` (`custom_minimum_size = Vector2(850, 0)`, h/v centered) → inner `MarginContainer` (margin 20) → `VBoxContainer` (Title Label font_size 32 centered, Separator, body, Separator, Buttons HBox h-center).
- **Scrollable list pattern (copy verbatim).** `CreateSavedServerPage.tscn` already has the exact list frame this page needs: `PanelContainer (ListPanel)` → `MarginContainer (ListMargin, margin 10)` → `ScrollContainer (v-expand)` → `MarginContainer (ScrollMargin, margin 10)` → `VBoxContainer (SavesListContainer, custom_minimum_size = Vector2(300,0), h-center)`. Rows are `Button`s added at runtime. Mirror this node-for-node for `ServersListContainer`.
- **`[Child]` resolution is by name.** The injected property name must match the node name in the `.tscn` (`[Child] public Button Foo` → a node named `Foo`). A mismatch compiles but throws at `_Ready` injection time.
- **KnownServersService API** (`Scripts/Service/KnownServers/KnownServersService.cs`, exposed as `Services.KnownServers`, inited in `ClientRootStarter._Ready`):
  - `List<KnownServer> GetAll()` — returns the **live** list (mutations reflect on the returned reference; treat as read-only in the UI, call `Add`/`Remove` to mutate).
  - `void Add(KnownServer server)` — appends, persists. **No de-duplication of host:port.** Do not add a duplicate check in the service; check with `Exists` in the page before adding if you want to avoid dupes.
  - `void Remove(KnownServer server)` — removes by record equality, persists.
  - `bool Exists(string host, int? port)`.
  - `KnownServer` is `readonly record struct`-like — actually `public record KnownServer(string Host, int? Port, string Label)`. `Port` is `int?` (`null` = default port). Equality is structural (record), so `Remove` matches by value.
- **Connect service signature** (`Scripts/Service/MainSceneService.cs`, exposed as `Services.MainScene`):
  - `ConnectToMultiplayerGame(string host = null, int? port = null)` — `port` is `int?`; pass `null` to let the server pick (spec #12: direct-connect field with no port → `null`).
  - **No gate yet.** Phase 5 wraps this call in `TryStartGame(...)`. In Phase 4 call the service directly. Leave a `// TODO (#16 gate, Phase 5)` comment on the connect handler so Phase 5 finds it.
- **Inline error pattern.** To show an error and return to the current page, push a `MessagePage`: `GoNext(PagesProvider.PrepareMessagePage(Tr("...KEY...")))`. The user clicks OK and lands back. This is exactly how `ConnectPage.ParseAndConnectToServer` reports a missing host — port the pattern.
- **`ConnectPage` is referenced only by** `PagesProvider.ConnectionPageScene`, `MultiplayerPage.ConnectButton`, and `MainMenu.tscn` (the `[ext_resource]` for `ConnectionPageScene`). All three are touched in this phase. `CONNECT_MENU__*` locale keys become orphaned — leave them for Phase 8 cleanup (matches the Phase 3 precedent for `HOST_MENU__*`).
- **`int.TryParse` on port.** `SpinBox.Value` is `double`; cast with `(int)`. For the direct-connect `LineEdit`, parse `host:port` with `String.Split(':')` and `int.TryParse`. Spec #12: if the user types only a host (no `:port`), port is `null`.
- **Locale convention.** `SECTION__KEY`, three files: `Assets/Locales/messages.pot` (empty `msgstr`), `en.po`, `ru.po`. `Tr()` is identity for unknown keys, so a missing translation renders the key verbatim — but all keys must still be added to all three files.

---

## Task 4.1 — `ServerListPage`

### 4.1a — `Pages/ServerList/ServerListPage.cs` (new)

```csharp
using System;
using System.Globalization;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scripts.Service.KnownServers;

namespace NeonWarfare.Scenes.Screen.NewMenu.MainMenu.Pages.ServerList;

public partial class ServerListPage : MainMenuPage
{
    // Known-servers list
    [Child] public VBoxContainer ServersListContainer { get; private set; }
    [Child] public Button RemoveServerButton { get; private set; }

    // Inline add form
    [Child] public LineEdit AddHostLineEdit { get; private set; }
    [Child] public SpinBox AddPortSpinBox { get; private set; }
    [Child] public LineEdit AddLabelLineEdit { get; private set; }
    [Child] public Button AddServerButton { get; private set; }

    // Direct connect
    [Child] public LineEdit DirectHostLineEdit { get; private set; }
    [Child] public Button ConnectButton { get; private set; }
    [Child] public Button BackButton { get; private set; }

    private KnownServer _selectedServer;

    public override void _Ready()
    {
        Di.Process(this);

        AddServerButton.Pressed += OnAddServer;
        RemoveServerButton.Pressed += OnRemoveServer;
        ConnectButton.Pressed += OnConnectDirect;
        BackButton.Pressed += () => GoBack();

        AddPortSpinBox.Value = Consts.DefaultPort;
        AddPortSpinBox.MaxValue = 65535;
        AddPortSpinBox.MinValue = 1;

        // Start with no row selected → Remove disabled until a row is clicked.
        RemoveServerButton.Disabled = true;
        _selectedServer = null;

        PopulateServersList();
    }

    private void PopulateServersList()
    {
        foreach (var child in ServersListContainer.GetChildren())
        {
            child.QueueFree();
        }

        foreach (var server in Services.KnownServers.GetAll())
        {
            var button = new Button();
            button.Text = String.IsNullOrEmpty(server.Label)
                ? $"{server.Host}:{server.Port?.ToString() ?? ""}"
                : $"{server.Label} ({server.Host}:{server.Port?.ToString() ?? ""})";
            button.Pressed += () =>
            {
                _selectedServer = server;
                RemoveServerButton.Disabled = false;
            };
            // Visually mark the current selection.
            if (_selectedServer is not null && _selectedServer == server)
            {
                button.ButtonPressed = true;
            }
            ServersListContainer.AddChild(button);
        }
    }

    private void OnAddServer()
    {
        string host = AddHostLineEdit.Text?.Trim();
        if (String.IsNullOrWhiteSpace(host))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
            return;
        }

        int? port = (int) AddPortSpinBox.Value == Consts.DefaultPort
            ? (int?) AddPortSpinBox.Value
            : (int) AddPortSpinBox.Value; // any explicit port is stored as-is
        string label = AddLabelLineEdit.Text?.Trim() ?? String.Empty;

        if (Services.KnownServers.Exists(host, port))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__ALREADY_EXISTS_ERROR")));
            return;
        }

        Services.KnownServers.Add(new KnownServer(host, port, label));

        // Clear the add form.
        AddHostLineEdit.Text = String.Empty;
        AddLabelLineEdit.Text = String.Empty;
        AddPortSpinBox.Value = Consts.DefaultPort;

        PopulateServersList();
    }

    private void OnRemoveServer()
    {
        if (_selectedServer is null)
        {
            return;
        }

        Services.KnownServers.Remove(_selectedServer);
        _selectedServer = null;
        RemoveServerButton.Disabled = true;
        PopulateServersList();
    }

    private void OnConnectDirect()
    {
        // TODO (#16 gate, Phase 5): wrap this call in TryStartGame(...).
        string raw = DirectHostLineEdit.Text?.Trim();
        if (String.IsNullOrWhiteSpace(raw))
        {
            GoNext(PagesProvider.PrepareMessagePage(Tr("SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR")));
            return;
        }

        // Accept "host" or "host:port". No port → null (spec #12).
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

        // Auto-add to known servers if not already known.
        if (!Services.KnownServers.Exists(host, port))
        {
            Services.KnownServers.Add(new KnownServer(host, port, String.Empty));
        }

        Services.MainScene.ConnectToMultiplayerGame(host, port);
    }
}
```

Decisions baked in:

- **Three actions, one page** (list + inline add + direct connect) rather than a separate Add page. The general plan allowed "inline-form host+port+label **or** separate page"; inline keeps everything on one screen and matches the form-shell template.
- **Selection model = click-to-select, Remove disabled until selected.** Mirrors `CreateSavedServerPage`'s save selection. No double-click-to-connect — the spec separates list (select) from direct connect; a row's Connect would be a third button. Keep the row a select-only `Button`; connecting always goes through the Connect button (which reads the direct-connect field). This is the simplest model that satisfies the spec. **If you prefer a per-row Connect**, swap the row `Button.Text` handler to also call `OnConnectRow(server)` — but the default plan above does not.
- **Add uses a `SpinBox` for port** (not a `LineEdit`) — consistent with `ConnectPage.PortSpinBox` and `CreateNewServerPage.PortSpinBox`. Range 1–65535. Stored as-is.
- **Duplicate-add guard via `Exists`.** The service itself does not de-duplicate (Phase 2 decision); the page guards so the list stays clean. On a duplicate, show an inline error via `PrepareMessagePage` and return.
- **Direct-connect parses `host:port` with a last-colon split** so IPv6-ish hosts without brackets still parse the trailing port. Spec #12: no `:port` → `port = null`.
- **Auto-add on connect.** The general plan: "Connect: ... + автодобавление в known если нового". Implemented as an `Exists` check before `Add`. Applies to the direct-connect path; the list path connects the selected server (already known).
- **Start handler calls the service directly** (no gate) — gate lands in Phase 5.
- **Port formatting in row label**: `server.Port?.ToString() ?? ""` so a null port renders as `host:` (empty after the colon). Acceptable; if you want `host` with no colon when port is null, adjust the ternary — not load-bearing.

### 4.1b — `Pages/ServerList/ServerListPage.tscn` (new)

Copy `Pages/Connect/ConnectPage.tscn` as the starting point for the outer shell, then graft the list frame from `CreateSavedServerPage.tscn`. Structure:

```
ServerListPage (Control, full-rect, script=ServerListPage.cs)
└─ MarginContainer (margin 20)
   └─ PanelContainer (custom_minimum_size = Vector2(850, 0), h/v centered)
      └─ MarginContainer (margin 20)
         └─ VBoxContainer
            ├─ Title Label  text="SERVER_LIST_MENU__TITLE"  (label_settings font_size 32, horizontal_alignment = 1)
            ├─ Separator (Control, custom_minimum_size = Vector2(0, 20))
            ├─ ServersListPanel (PanelContainer, v-expand)        ← list frame, copy from CreateSavedServerPage.ListPanel
            │   └─ ListMargin (MarginContainer, margin 10)
            │      └─ ScrollContainer (v-expand)
            │         └─ ScrollMargin (MarginContainer, margin 10)
            │            └─ ServersListContainer (VBoxContainer, custom_minimum_size = Vector2(300, 0), h-center)
            ├─ Separator (Control, custom_minimum_size = Vector2(0, 10))
            ├─ AddServerContainer (VBoxContainer)
            │   ├─ AddServerLabel  text="SERVER_LIST_MENU__ADD_SERVER_TITLE"
            │   ├─ AddHostContainer (HBox)
            │   │   ├─ AddHostLabel  text="SERVER_LIST_MENU__HOST"
            │   │   └─ AddHostLineEdit  (custom_minimum_size = Vector2(300, 0), placeholder_text="SERVER_LIST_MENU__HOST_PLACEHOLDER")
            │   ├─ AddPortContainer (HBox)
            │   │   ├─ AddPortLabel  text="SERVER_LIST_MENU__PORT"
            │   │   └─ AddPortSpinBox  (min_value=1, max_value=65535, value=25566)
            │   ├─ AddLabelContainer (HBox)
            │   │   ├─ AddLabelLabel  text="SERVER_LIST_MENU__LABEL"
            │   │   └─ AddLabelLineEdit  (custom_minimum_size = Vector2(300, 0), placeholder_text="SERVER_LIST_MENU__LABEL_PLACEHOLDER")
            │   └─ AddServerButton  text="SERVER_LIST_MENU__ADD_BUTTON"  (custom_minimum_size = Vector2(200, 40))
            ├─ RemoveServerContainer (HBox, h-center)
            │   └─ RemoveServerButton  text="SERVER_LIST_MENU__REMOVE_BUTTON"  (custom_minimum_size = Vector2(200, 40))
            ├─ Separator (Control, custom_minimum_size = Vector2(0, 20))
            ├─ DirectConnectContainer (VBoxContainer)
            │   ├─ DirectConnectLabel  text="SERVER_LIST_MENU__DIRECT_CONNECT_TITLE"
            │   └─ DirectHostLineEdit  (custom_minimum_size = Vector2(400, 0), placeholder_text="SERVER_LIST_MENU__DIRECT_HOST_PLACEHOLDER")
            ├─ Separator (Control, custom_minimum_size = Vector2(0, 20))
            └─ Buttons (HBox, h-center)
               ├─ ConnectButton  text="SERVER_LIST_MENU__CONNECT_BUTTON"  (custom_minimum_size = Vector2(200, 50))
               └─ BackButton     text="GENERIC_MENU__BACK_BUTTON"          (custom_minimum_size = Vector2(200, 50))
```

Node names must match the `[Child]` properties in 4.1a **exactly**: `ServersListContainer`, `RemoveServerButton`, `AddHostLineEdit`, `AddPortSpinBox`, `AddLabelLineEdit`, `AddServerButton`, `DirectHostLineEdit`, `ConnectButton`, `BackButton`.

Simplest path: **copy `ConnectPage.tscn` into the new folder, reattach the script to `ServerListPage.cs`, rename the root node to `ServerListPage`, then add the list subtree (copy `ListPanel`…`SavesListContainer` from `CreateSavedServerPage.tscn`, renaming `SavesListContainer` → `ServersListContainer`) and the Add/Direct-connect subtrees.** Easier in the Godot editor than hand-authoring the `.tscn`.

---

## Task 4.2 — Repoint `MultiplayerPage.ConnectButton` + rewire `PagesProvider`

### 4.2a — `Pages/Multiplayer/MultiplayerPage.cs` (edit)

Change the `ConnectButton` handler. Remove the Phase-3 temporary-comment block and point at the new scene.

Before (Phase 3 state):
```csharp
// Connect targets the existing ConnectPage for now; Phase 4 repoints this to ServerListPage
// and deletes ConnectPage. Do not "fix" this until Phase 4.
ConnectButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ConnectionPageScene));
```
After:
```csharp
ConnectButton.Pressed += () => GoNext(PagesProvider.PreparePage(PagesProvider.ServerListPageScene));
```

### 4.2b — `PagesProvider.cs` (edit)

Rename the `ConnectionPageScene` export to `ServerListPageScene`:

```csharp
[Export] [NotNull] public PackedScene MainPageScene { get; private set; }
[Export] [NotNull] public PackedScene SettingsPageScene { get; private set; }
[Export] [NotNull] public PackedScene ServerListPageScene { get; private set; }
[Export] [NotNull] public PackedScene MessagePageScene { get; private set; }
[Export] [NotNull] public PackedScene LanguageSelectionPageScene { get; private set; }
[Export] [NotNull] public PackedScene SingleplayerPage { get; private set; }
[Export] [NotNull] public PackedScene MultiplayerPageScene { get; private set; }
[Export] [NotNull] public PackedScene CreateNewServerPageScene { get; private set; }
[Export] [NotNull] public PackedScene CreateSavedServerPageScene { get; private set; }
```

(Removed: `ConnectionPageScene`. Added: `ServerListPageScene` in its slot.) `PreparePage(PackedScene)` needs no change — it is generic over `MainMenuPage`.

> **Rename vs. keep name.** `PagesProvider.ConnectionPageScene` could be left named as-is and just pointed at the new `.tscn` (it is only a `PackedScene` slot). Renaming to `ServerListPageScene` is clearer and matches the Phase-3 "no misleading names" stance (Phase 3 renamed `CreateServerPageScene` → `CreateNewServerPageScene` rather than repoint). Prefer the rename.

### 4.2c — `MainMenu.tscn` (edit) — rewire the `PagesProvider` node

On the `PagesProvider` node (currently `MainMenu.tscn` lines 32–42):

1. **Remove** the `[ext_resource ... ConnectPage.tscn ...]` entry (currently `id="5_lc56h"`, line 8) **and** the `ConnectionPageScene = ExtResource("5_lc56h")` assignment (line 36).
2. **Add** one new `[ext_resource]` entry for `Pages/ServerList/ServerListPage.tscn`, and one matching assignment on the `PagesProvider` node: `ServerListPageScene = ExtResource("<new id>")`.
3. Net ext_resource count: −1 +1 = 0 change, so `load_steps` stays 17. (Verify in the editor after saving.)

Easiest done in the Godot editor: open `MainMenu.tscn`, select the `PagesProvider` node, drag `ServerListPage.tscn` into the new `ServerListPageScene` export slot, clear the old `ConnectionPageScene` assignment, save. The editor rewrites the `.tscn` correctly (uid/path/id/load_steps). Hand-editing is error-prone — prefer the editor.

---

## Task 4.3 — Delete `ConnectPage`

Delete both files:
- `Scenes/Screen/NewMenu/MainMenu/Pages/Connect/ConnectPage.cs`
- `Scenes/Screen/NewMenu/MainMenu/Pages/Connect/ConnectPage.tscn`
- Remove the now-empty `Connect/` directory (and its `.uid` files if Godot left any).

**Pre-check before deleting** (catch any reference missed above):
```bash
grep -rn "ConnectPage\|ConnectionPageScene\|Pages.Connect\b" --include=*.cs --include=*.tscn .
```
Expected after Tasks 4.2a + 4.2b: **zero hits.** If any remain, do not delete — resolve the reference first. (Expected hit count before those tasks: the two `PagesProvider`/`MainMenu.tscn` references from 4.2b and the one `MultiplayerPage` reference from 4.2a.)

> **`CONNECT_MENU__*` locale keys become orphaned** once `ConnectPage` is deleted. **Leave them in place** for Phase 4 (removing them now is unrelated churn); Phase 8's locale pass removes them along with `HOST_MENU__*` (orphaned in Phase 3). Flagged so the implementer doesn't "clean them up" speculatively — matches the Phase-3 precedent.

---

## Task 4.4 — Translation keys

Add **every** key below to **all three** files: `Assets/Locales/messages.pot` (`msgstr` empty), `Assets/Locales/en.po`, `Assets/Locales/ru.po`. Follow the existing format (blank line between entries). Group the new section near the existing `CONNECT_MENU__` / `MULTIPLAYER_MENU__` blocks.

| msgid | en msgstr | ru msgstr |
|---|---|---|
| `SERVER_LIST_MENU__TITLE` | `Connect to Server` | `Подключение к серверу` |
| `SERVER_LIST_MENU__ADD_SERVER_TITLE` | `Add server` | `Добавить сервер` |
| `SERVER_LIST_MENU__HOST` | `Host:` | `Хост:` |
| `SERVER_LIST_MENU__HOST_PLACEHOLDER` | `host name or IP` | `имя хоста или IP` |
| `SERVER_LIST_MENU__PORT` | `Port:` | `Порт:` |
| `SERVER_LIST_MENU__LABEL` | `Label:` | `Метка:` |
| `SERVER_LIST_MENU__LABEL_PLACEHOLDER` | `optional label` | `необязательная метка` |
| `SERVER_LIST_MENU__ADD_BUTTON` | `Add` | `Добавить` |
| `SERVER_LIST_MENU__REMOVE_BUTTON` | `Remove` | `Удалить` |
| `SERVER_LIST_MENU__DIRECT_CONNECT_TITLE` | `Direct connect` | `Прямое подключение` |
| `SERVER_LIST_MENU__DIRECT_HOST_PLACEHOLDER` | `host:port` | `хост:порт` |
| `SERVER_LIST_MENU__CONNECT_BUTTON` | `Connect` | `Подключиться` |
| `SERVER_LIST_MENU__HOSTNAME_EMPTY_ERROR` | `Enter a host name` | `Введите имя хоста` |
| `SERVER_LIST_MENU__ALREADY_EXISTS_ERROR` | `This server is already in the list` | `Этот сервер уже в списке` |

Notes:
- `GENERIC_MENU__BACK_BUTTON` already exists (added in Phase 3) — reuse, do not re-add.
- Do **not** remove `CONNECT_MENU__*` keys here (Phase 8 cleanup).

---

## Task 4.5 — Verify

1. Confirm the new files exist:
   - `Pages/ServerList/ServerListPage.cs` + `.tscn`
2. Confirm `ConnectPage.cs`, `ConnectPage.tscn`, and the `Connect/` folder are gone, and the grep in 4.3 returns zero hits.
3. Confirm `PagesProvider.cs` compiles (has `ServerListPageScene`, no `ConnectionPageScene`), and `MultiplayerPage.cs` compiles (Connect handler points at `ServerListPageScene`).
4. Build from the repository root:
   ```bash
   dotnet build
   ```
   Expect: **0 errors.** Warnings acceptable but read them — a `CS0114` (hide) or `CS0103` (name not found) usually means a `[Child]` name doesn't match its node name in the `.tscn`.
5. **Open `MainMenu.tscn` in the Godot editor** and confirm the `PagesProvider` node has `ServerListPageScene` assigned (no empty export slot — an empty `[NotNull]` export throws at startup). This is blocking: a missed assignment passes `dotnet build` but crashes on launch.
6. The implementer is **not** required to play through the game in Phase 4, but a quick editor run to confirm the menu navigates `Main → Multiplayer → Connect → ServerListPage`, that Add/Remove mutate the list, and that the direct-connect field starts a connection, is the recommended sanity check. (`Services.MainScene.ConnectToMultiplayerGame` will try a real connection; a failed connection to a bogus host is fine — the goal is that the call is reached without a C# exception.)

---

## Out of scope for Phase 4 (do NOT do)

- **First-run gate (#16)** — Phase 5. The connect handler calls the service directly; the `// TODO (#16 gate, Phase 5)` comment marks the wrap point.
- **`SettingsHubPage` / category pages** — Phase 7. `MainPage.SettingsButton` keeps pointing at the existing `SettingsPage`.
- **Removing `CONNECT_MENU__*` locale keys** — Phase 8 cleanup. Leave orphaned keys in place.
- **Per-row Connect button in the servers list** — the spec separates list (select) from direct connect; the default plan keeps the row select-only. If you add a per-row Connect, do it as an extension of the plan above, not a redesign.
- **Renaming `SingleplayerPage` export** to `SingleplayerPageScene` to match the convention — cosmetic, not in this phase's scope.
- **Changing `MainMenu.tscn`'s 3D background / `PageContainer` wiring** — untouched.

---

## Gotchas recap

- **`ServerListPage` is a form, not a hub.** Use the PanelContainer shell (copy from `ConnectPage.tscn` / `CreateSavedServerPage.tscn`), not the MainPage neon-button template. The list frame copies node-for-node from `CreateSavedServerPage.tscn` (`ListPanel`…`SavesListContainer`, renamed to `ServersListContainer`).
- **`[Child]` resolves by node name.** Every `[Child] public X Foo` needs a node named exactly `Foo` in the `.tscn`. A mismatch compiles but throws at `_Ready` injection time.
- **`ConnectionPageScene` in `PagesProvider` is ConnectPage, not a "server list" page.** Rename to `ServerListPageScene` and repoint; do not leave the misleading name.
- **Hand-editing `MainMenu.tscn` ext-resources is fragile.** Do the `PagesProvider` rewiring in the Godot editor; it manages uid/path/load_steps correctly. (Phase 3's implementer did this in the editor.)
- **`KnownServer.Port` is `int?`.** When formatting a row label, handle `null` (`server.Port?.ToString() ?? ""`). When connecting from a host-only direct-connect string, pass `port = null` — do not default it to `25566` silently.
- **`KnownServersService.GetAll()` returns the live list.** Call `Add`/`Remove` to mutate (they persist); iterate the snapshot when rendering rows.
- **No duplicate check in the service** — Phase 2 decision. The page checks `Exists` before `Add` so the list stays clean; do not push the de-dup down into the service.
- **`ConnectPage` deletion is in scope here** (not Phase 8) because its replacement lands in this same phase. `HOST_MENU__*` deletion was Phase 3 (same rationale); `CONNECT_MENU__*` deletion is Phase 8.
- **Leave a `// TODO (#16 gate, Phase 5)` on the connect handler** (`ServerListPage.OnConnectDirect`). Phase 5 greps for these — Phase 3 left the same marker on the host handlers.
- **`CONNECT_MENU__*` keys stay** for now; removing them is Phase 8 cleanup alongside `HOST_MENU__*`.
