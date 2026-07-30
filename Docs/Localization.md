# Localization

[← Project README](../README.md)

The translation files are `Assets/Locales/en.po` and `ru.po`, the template is `messages.pot`. A new
key is added to all three files.

Keys are written in `SCREAMING_SNAKE_CASE` and grouped by screen with a double underscore:
`MAIN_MENU__EXIT_BUTTON`, `HUD__CHAT_PLACEHOLDER`, `CONNECT_MENU__HOSTNAME_UNSPECIFIED_ERROR`.

## How a translation is substituted

| Where the text is | What to write |
|---|---|
| In a scene (`.tscn`) | The key directly in the text field — Godot substitutes the translation itself |
| In node code | `Tr("KEY")` — a `Node` method, available without extra dependencies |
| In code outside a node | `Services.I18N.Tr("KEY")` (example: `LoadingScreenTypes`) |

Most of the text lives right in the scenes; calling `Tr(...)` from code is only needed where the
string is assembled dynamically.

> [!NOTE]
> **Chat command responses are not localized.**
>
> This covers all of `Scenes/World/Service/Command/Impl/`, plus `RequireAdminMessage` in
> `WorldCommandService`. Commands are an admin tool that is used extremely rarely, so keeping their
> texts in `.po` makes no sense.

## Locale selection

The current locale is set in `RootStarter` **after** the settings are loaded but **before** the
loading screen is first shown — otherwise the loading screen will show keys instead of text
(see [Startup flow](Arch/Startup-flow.md)).

The default locale is taken from the OS (`Services.I18N.GetUserOsLocaleInfoOrDefault()`). The
language can be changed in-game on the `LanguageSelectionPage` page.

## Preview in the editor

Above the scene, in the "View → Preview Translation" drop-down menu, you can select the languages the
project's scenes will be displayed in inside the editor (this does not affect the game). If no
language is selected (the "none" item), the localization keys are shown.
