# UI

[← Project README](../../README.md)

## The main menu: the page stack

`Scenes/Screen/NewMenu/`:

* `PageContainer` — holds the stack, can do `SetRootPage` / `PushPage` / `PopPage`, catches cycles;
* `Page` / `IPage` — a page with `Parent`/`Child` links and the `OnShown` / `OnHidden` / `Close`
  callbacks;
* `MainMenuPage` — the base for the menu pages, receives `PagesProvider` through
  `WithAvailablePages(...)`;
* `PagesProvider` — the `PackedScene` storage of all the pages (Main, Settings, Connect, Host,
  Singleplayer, Message, LanguageSelection); a descendant of `CheckedAbstractStorage`, the references
  are set in the editor.

## The settings screen

It is built **from a model, not by hand**. `MenuGameSettings`
(`Scenes/Screen/NewMenu/SettingsSystem/`) describes the fields, and the `[Name]`, `[Hint]`, `[Hide]`,
`[Range]`, `[Step]` attributes control the display. `GetVisibleSettings()` assembles a list of
`Setting` by reflection, from which `SettingsPage` generates the controls.

To add a setting it is enough to add a field to the model and annotate it with attributes — there is
no need to touch `SettingsPage` by hand.

## The in-game HUD and the loading screen

`Hud` (the client) and `ServerHud` (the server console) — both receive the `World` through
`InitPreReady(world)` **before** being added to the tree, because they need it earlier than
`_Ready()`. Which of the two to create is decided by the game starter (see
[Startup flow](Startup-flow.md)).

The loading screen (`LoadingScreen`) lives in a separate `CanvasLayer` at the very top and supports an
optional cancel button — the connection to a server uses it, so that the wait can be interrupted.
