---
id: main-menu-structure
title: "План реструктуризации MainMenu (эпик #12): структура, фазы, допущения"
category: decision
status: active
tags: [main-menu, ui, plan, issue-12, issue-94, issue-16]
created: "2026-08-07T12:50:59"
updated: "2026-08-07T12:51:17"
---

<!-- compiled_truth -->
План эпика #12 (Creation of MainMenu), записан в `plans/main-menu-general-plan.md`. Скоуп: реструктуризация меню по спеке issue #12 + сервер-лист (часть #94) + 5 категорий настроек + first-run gate #16.

## Решения пользователя по 3 развилкам (важны для будущих контекстов)

1. **Server list** — ПОЛНЫЙ список известных серверов ДЕЛАЕТСЯ в этом эпике (а не только поле IP:порт по спеке #12). Часть #94 закрывается внутри #12.
2. **Settings** — ПОЛНЫЕ 5 категорий по #12 (Игрок/Управление/Интерфейс/Графика/Аудио) с конкретными контролами (dropdown разрешения, слайдеры громкости 0-100, диалог несохранённых изменений, кастомный color picker). Не «только навигация», не «не трогать».
3. **First-run gate #16** — ВКЛЮЧЁН в эпик. Персистентный флаг `PlayerSettingsAcknowledged` в `game-settings.json`, gate перед каждым запуском игры.

## Целевая структура меню

```
MainPage
├─ Resume (если есть LastGame)
├─ Одиночная игра → SingleplayerPage (TabContainer new/load)
├─ Игра по сети (НОВОЕ, MultiplayerPage хаб)
│   ├─ Создать новую игру → CreateNewServerPage
│   ├─ Создать из сохранения → CreateSavedServerPage (со списком сейвов + поиск)
│   └─ Подключиться к серверу → ServerListPage (список + add/remove/connect)
├─ Настройки → SettingsHubPage (НОВОЕ, 5 категорий)
│   ├─ Игрок / Управление / Интерфейс / Графика / Аудио
├─ Смена языка → LanguageSelectionPage
└─ Выход
```

PlayerSettings gate (#16): перед single/multi new/connect проверять `GameSettings.PlayerSettingsAcknowledged`. Если false → push PlayerSettingsPage (ник+цвет), save снимает флаг → continuation-action запускает игру.

## 8 фаз (после каждой — `dotnet build`, 0 ошибок)

1. **Фундамент SettingsSystem + persistence** — расширить `GameSettings`/`MenuGameSettings` новыми полями (PlayerSettingsAcknowledged, MasterVolume/SoundsVolume/MusicVolume, Fullscreen, Resolution, InterfaceSize); добавить атрибуты `[Category]` + `[Options]` для категоризации и dropdown; `MenuGameSettingsService.Convert` проброс полей.
2. **Сервис known-servers** — `Scripts/Service/KnownServersService.cs` (record KnownServer, `user://known-servers.json`, методы GetAll/Add/Remove/Exists); регистрация в `Services.cs`.
3. **Multiplayer хаб + серверные формы** — MultiplayerPage, CreateNewServerPage (перенос логики HostPage), CreateSavedServerPage (список сейвов + динамический поиск по TextChanged); MainPage → один MultiplayerButton вместо Host+Connect.
4. **ServerListPage + удаление ConnectPage** — список известных серверов, add/remove/connect, прямое подключение «host:port».
5. **First-run gate** — PlayerSettingsPage (modal-style, continuation-action), `TryStartGame(Action)` helper, обернуть 3 точки старта игры.
6. **Кастомный ColorPicker** — `ColorPickerPanel.cs` (ColorPickerButton + hex LineEdit + preset-палитра); замена Color-configurator.
7. **Settings хаб + 5 категорий + ConfirmDialog** — SettingsHubPage, обобщённый SettingsCategoryPage, ConfirmDialogPage (Reset/Back/Continue), dirty-проверка через JSON-сравнение; удаление старого SettingsPage.
8. **Локаль + полировка** — ключи в messages.pot/en.po/ru.po (конвенция `SECTION__KEY`, `SETTING_MENU__` единственное число); ручная верификация в Godot-редакторе.

## Ключевые технические решения

- **`[Options]` attribute vs тип-словарь Configurators:** Options не тип — регистрация по типу невозможна. Решение: pre-check атрибута в `SettingContainer._Ready` перед тип-конфигуратором, строить `OptionButton`.
- **Dirty-проверка настроек:** сравнение `_draftSettings.Serialize()` vs `_preservedSettings.Serialize()` (JSON-строка). Простой и надёжный.
- **PlayerSettingsPage dual-role:** один компонент для gate-modal и settings-category, поведение по наличию continuation-action. Если усложнит — разделить на 2 класса.
- **Page-архитектура НЕ трогается** — `Page`/`PageContainer`/`IPage` (push/pop) рабочая. DI: `[Export][NotNull]` + `Di.Process(this)` (NotNullChecker отсутствует в проекте).
- **Visual:** неон-cyan, фон 3D-коридор; MainPage кнопки = StyleBoxEmpty+cyan gradient hover, Play-Bold; страницы = PanelContainer margin 20.

## Допущения / риски

- MainPage Resume не в спеке #12, но уже работает — оставляю.
- AutoScale (#137, открытый) НЕ в этом эпике; InterfaceSize настройка добавляется, реальный scaling-mechanism отдельно.
- GameSettings record immutable — обновление через `with`; персист в `user://`.

## Связанные страницы

- Архитектура клиент/сервер — [[client-server-subsystem-split]]
- Предыдущая миграция визуала (завершена) — [[main-menu-visual-migration]]
- Single authoritative logic path (single/host/dedicated один серверный путь) — [[single-authoritative-logic-path]]


## Timeline

- time: 2026-08-07T12:50:59
  kind: decision
  summary: "Created this page: План реструктуризации MainMenu (эпик #12): структура, фазы, допущения"
  source: "Планирование эпика #12 с пользователем; план записан в plans/main-menu-general-plan.md"
  affects: [main-menu-structure]

- time: 2026-08-07T12:51:17
  kind: decision
  summary: "Записан полный план реструктуризации MainMenu (эпик #12) в plans/main-menu-general-plan.md; скоуп расширен до #94 сервер-лист + #16 first-run gate по решению пользователя"
  source: "Обсуждение структуры меню + ответы пользователя на 3 развилки (server-list / settings categories / first-run gate)"
  affects: [main-menu-structure]
