# Интерфейс

[← README проекта](../../README.md)

## Главное меню: стек страниц

`Scenes/Screen/NewMenu/`:

* `PageContainer` — держит стек, умеет `SetRootPage` / `PushPage` / `PopPage`, ловит циклы;
* `Page` / `IPage` — страница со связями `Parent`/`Child` и колбэками `OnShown` / `OnHidden` / `Close`;
* `MainMenuPage` — база для страниц меню, получает `PagesProvider` через `WithAvailablePages(...)`;
* `PagesProvider` — хранилище `PackedScene` всех страниц (Main, Settings, Connect, Host, Singleplayer,
  Message, LanguageSelection); наследник `CheckedAbstractStorage`, ссылки проставляются в редакторе.

## Экран настроек

Строится **из модели, а не вручную**. `MenuGameSettings`
(`Scenes/Screen/NewMenu/SettingsSystem/`) описывает поля, а атрибуты `[Name]`, `[Hint]`, `[Hide]`,
`[Range]`, `[Step]` управляют отображением. `GetVisibleSettings()` рефлексией собирает список
`Setting`, по которому `SettingsPage` генерирует контролы.

Чтобы добавить настройку, достаточно добавить поле в модель и разметить его атрибутами — руками
трогать `SettingsPage` не нужно.

## Игровой HUD и экран загрузки

`Hud` (клиент) и `ServerHud` (консоль сервера) — оба получают `World` через `InitPreReady(world)` **до**
добавления в дерево, потому что он нужен им раньше `_Ready()`. Какой из двух создавать, решает стартер
игры (см. [Поток запуска](Startup-flow.md)).

Экран загрузки (`LoadingScreen`) живёт в отдельном `CanvasLayer` на самом верху и поддерживает
опциональную кнопку отмены — ею пользуется подключение к серверу, чтобы можно было прервать ожидание.
