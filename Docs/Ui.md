# UI

[← Project README](../README.md)

## The main menu

`Scenes/Screen/NewMenu/`. `MainMenu` (`MainMenu.tscn`) is the scene the `MainSceneService` puts into
`MainSceneContainer`. It holds three things:

* `PagesProvider` — the `PackedScene` storage of every page, references are set in the editor;
* the animated 3D background (`SubViewportContainer` → `Background3D` + `CorridorContainer`);
* `PageContainer` — the page stack; `MainMenu._Ready()` gives it `MainPage` as the root page.

`MainMenu` exposes `PushPage(IPage)` / `PopPage()` / `CurrentPage` — that is how code outside the menu
(for example `MainSceneService.StartMainMenu(message)`) adds a page.

### The page stack

`Scenes/Screen/NewMenu/PagesSystem/`:

* `PageContainer` — `SetRootPage` / `PushPage` / `PopPage`; only one page is in the tree at a time
  (pushing removes the previous one and adds the next), catches cycles and pushes of non-top pages;
* `IPage` / `Page` — `Parent`/`Child` links, `IsRoot`/`IsTop`, the `OnShown` / `OnHidden` / `Close`
  callbacks and `Setup(goBack, next)`, which the container uses to hand the page its `GoBack` /
  `GoNext` delegates;
* `MainMenuPage` — the base class of every menu page: it stores the `PagesProvider` (passed through
  `WithAvailablePages(...)`) and provides `TryStartGame(startAction)`.

`TryStartGame` is the first-run gate: if `PlayerSettingsAcknowledged` is not set yet, it pushes
`PlayerSettingsPage` with `startAction` as the post-save continuation instead of starting the game.
Every entry point into a game session goes through it.

### The pages

`Scenes/Screen/NewMenu/MainMenu/Pages/`:

| Page | What it does |
|---|---|
| `MainPage` | Root. Resume (hidden when `Services.LastGame` has nothing), Singleplayer, Multiplayer, Settings, Language, Quit |
| `SingleplayerPage` | New game / load game tabs, save name, list of saves → `MainScene.StartSingleplayerGame` |
| `MultiplayerPage` | A hub: create a new server, create from a save, connect |
| `CreateNewServerPage` | Port, new save name, "dedicated" flag → `MainScene.HostMultiplayerGameAsClient` |
| `CreateSavedServerPage` | The same, but over an existing save with a search filter |
| `ServerListPage` | `Services.KnownServers`: the list, the inline add form, remove, and direct `host[:port]` connect |
| `SettingsHubPage` | Buttons into the five setting categories: Player, Controls, Interface, Graphics, Audio |
| `SettingsCategoryPage` | The generated settings of one category, Save / Cancel / Back |
| `PlayerSettingsPage` | The `Player` category alone; used by the first-run gate, sets `PlayerSettingsAcknowledged` on save |
| `LanguageSelectionPage` | Radio buttons over `Services.I18N.Locales`, applies the locale immediately, Save writes it into `GameSettings` |
| `ConfirmDialogPage` | Three-button confirmation (Reset / Back / Continue), used for unsaved settings |
| `MessagePage` | A message with an OK button; created through `PagesProvider.PrepareMessagePage(text)` |

Pages that need arguments are created through the `PagesProvider.Prepare*` methods
(`PrepareMessagePage`, `PreparePlayerSettingsPage`, `PrepareSettingsCategoryPage`,
`PrepareConfirmDialogPage`); the rest through `PreparePage(packedScene)`. All of them return a page
that already has the `PagesProvider`. Arguments are stashed in a `Configure` / `Setup` method and
applied in `_Ready()` — the child nodes do not exist earlier.

## The settings screen

It is built **from a model, not by hand**. `MenuGameSettings`
(`Scenes/Screen/NewMenu/SettingsSystem/`) describes the fields; the attributes control the display:

| Attribute | Effect |
|---|---|
| `[Category]` | Which `SettingsHubPage` category the field belongs to |
| `[Name]`, `[Hint]` | Localization keys for the label and the hint |
| `[Hide]` | Excluded from the generated list (`PlayerSettingsAcknowledged`) |
| `[Range]`, `[Step]` | A numeric field becomes an `HSlider` (without `[Range]` — a `SpinBox`) |
| `[Options]` | A fixed set of values → an `OptionButton` |

`GetVisibleSettings(category)` assembles a list of `Setting` by reflection; `SettingsCategoryPage`
turns each of them into a `SettingContainer`, which picks the input control by field type through
`Configurators` (`bool` → `CheckBox`, numbers → slider/spinbox, `string` → `LineEdit`, `Color` →
`ColorPickerPanel` with a neon preset palette). To add a setting it is enough to add a field to the
model, to `GameSettings` and to both `Convert` methods of `MenuGameSettingsService` — the pages are
not touched.

`MenuGameSettings` is the **menu-side** model; the persisted one is `GameSettings`
(see [Data and saves](Data-and-saves.md)). `Services.MenuGameSettings` converts between them and
applies the runtime side-effects (window mode, resolution, audio bus volumes, locale).

Both settings pages use the same draft/preserved pattern: two copies of the settings are loaded,
edits go into the draft, Save applies and persists it, Cancel re-applies the preserved snapshot to
roll back the runtime side-effects. Back on `SettingsCategoryPage` compares the serialized copies and
pushes a `ConfirmDialogPage` when they differ.

## The in-game HUD and the loading screen

`Hud` (the client) and `ServerHud` (the server console) — both receive the `World` through
`InitPreReady(world)` **before** being added to the tree, because they need it earlier than `_Ready()`.
Which of the two to create is decided by the game starter (see [Startup flow](Startup-flow.md)).
Both are still debug-grade: performance counters, chat, save, test buttons; `Hud` additionally has
the exit-to-menu button.

The loading screen (`LoadingScreen`) lives in a separate `CanvasLayer` at the very top and supports an
optional cancel button — the connection to a server uses it, so that the wait can be interrupted.
`Services.LoadingScreen.SetLoadingScreen(text | type, cancelAction)` shows it, `Clear()` removes it.
