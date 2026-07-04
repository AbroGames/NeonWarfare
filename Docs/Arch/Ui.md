# Интерфейс

[← README проекта](../../README.md)

Главное меню построено на **стеке страниц** (`Scenes/Screen/NewMenu/`):

* `PageContainer` — держит стек, умеет `SetRootPage` / `PushPage` / `PopPage`, ловит циклы;
* `Page` / `IPage` — страница со связями `Parent`/`Child` и колбэками `OnShown` / `OnHidden` / `Close`;
* `MainMenuPage` — база для страниц меню, получает `PagesProvider` через `WithAvailablePages(...)`;
* `PagesProvider` — хранилище `PackedScene` всех страниц (Main, Settings, Connect, Host, Singleplayer,
  Message, LanguageSelection).

Экран настроек строится **из модели, а не вручную**: `MenuGameSettings` описывает поля, а атрибуты
`[Name]`, `[Hint]`, `[Hide]`, `[Range]`, `[Step]` управляют отображением. `GetVisibleSettings()` рефлексией
собирает список `Setting`, по которому `SettingsPage` генерирует контролы.

Игровой HUD: `Hud` (клиент) и `ServerHud` (консоль сервера) — оба получают `World` через `InitPreReady(world)`
до добавления в дерево. Экран загрузки (`LoadingScreen`) живёт в отдельном `CanvasLayer` на самом верху
и поддерживает опциональную кнопку отмены.
