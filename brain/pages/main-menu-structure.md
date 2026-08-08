---
id: main-menu-structure
title: "План реструктуризации MainMenu (эпик #12): структура, фазы, допущения"
category: decision
status: active
tags: [main-menu, ui, plan, issue-12, issue-94, issue-16]
created: "2026-08-07T12:50:59"
updated: "2026-08-07T17:17:53"
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
2. **Сервис known-servers** — `Scripts/Service/KnownServers/` (record KnownServer, KnownServersService, `user://known-servers.json`, Init/GetAll/Add/Remove/Exists); регистрация в `Services.KnownServers`, Init в ClientRootStarter.
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

## Layout-уроки кастомных контролов в SettingContainer (актуально для Phase 7+)

Phase 6 вскрыла два layout-капкана при возврате кастомного Control из Configurator в `SettingContainer._Ready`:

1. **`Control`-наследник НЕ агрегирует min-size детей.** В отличие от `Container`, голый `Control` не считает `_GetMinimumSize` детей. Если панель многострочная (палитра под инпутами) — задавай `CustomMinimumSize` явно в `_Ready`, иначе родитель строки не вырастет и контент обрежется/наползёт на соседние строки.
2. **`SettingContainer._Ready` перетирал высоту input-контрола:** `_inputControl.CustomMinimumSize = new Vector2(300, 20)` — жёсткие 20px по Y, игнорирует собственную min-height контрола. Фикс: `new Vector2(300, _inputControl.CustomMinimumSize.Y)` — уважать min-height контрола, ширину держать 300 для выравнивания. Любой будущий многострочный/многоэлементный конфигуратор должен это учитывать.
3. **Палитра swatch = `GridContainer` (6 колонок, wrap), НЕ `HBoxContainer`.** 12 swatch × ~30px в одну строку HBox не помещаются, вылезают за `SettingContainer` и наползают на соседние setting-строки. Grid с `Columns` переносит на новые ряды автоматически. Применимо к любому multi-item контролу внутри setting-строки.

Phase 6 факты: палитра = GridContainer 6×2, панель `CustomMinimumSize.Y = 92` (top-row 28 + sep 4 + 2 ряда swatch 56 + grid sep 4). `EditAlpha = false` (PlayerColor RGB-only). Single-source-of-truth `SetColor` + `_suppress` guard против рекурсивного `ColorChanged`. Hex `TextChanged` коммитит только при `Color.HtmlIsValid` (без сброса box mid-typing), `TextSubmitted`/`FocusExited` snap-back.

## Допущения / риски

- MainPage Resume не в спеке #12, но уже работает — оставляю.
- AutoScale (#137, открытый) НЕ в этом эпике; InterfaceSize настройка добавляется, реальный scaling-mechanism отдельно.
- GameSettings record immutable — обновление через `with`; персист в `user://`.

## Прогресс фаз

| Фаза | Статус |
|------|--------|
| 1 — Settings Foundation | ✅ завершена |
| 2 — Known Servers Service | ✅ завершена |
| 3 — Multiplayer хаб | ✅ завершена |
| 4 — ServerListPage | ✅ завершена |
| 5 — First-run gate | ✅ завершена |
| 6 — ColorPicker | ✅ завершена |
| 7 — Settings хаб | ✅ завершена |
| 8 — Локаль | ❌ не начата |

## Pages-system паттерны (PageContainer / Page / MainMenuPage)

Актуально для любой страницы поверх `Scenes/Screen/NewMenu/PagesSystem/`. Вскрыты Phase 7, но не специфичны для settings:

1. **Configure-before-`_Ready`.** Данные для страницы передаются через метод (`Configure(...)`, `Setup(...)`, `SetContinuation(...)`), вызываемый из `PagesProvider.PrepareXxxPage` ДО `PageContainer.PushPage` → `AddChild`. `PushPage` добавляет ноду в дерево после prepare, значит `_Ready` бежит после configure — порядок гарантирован. НЕ читать данные в конструкторе (узлов ещё нет), НЕ переносить configure-логику в `_Ready` (категория/title неизвестны в момент construction). Узлы `[Child]` НЕ существуют в момент `Setup` — сташь строки в поля, применяй в `_Ready` после `Di.Process(this)`.
2. **Callback / pop ordering при confirm-dialog поверх страницы.** Диалог пушится через `GoNext`; он становится `CurrentPage`, родительская страница `RemoveChild`'ится из дерева (но не freed). `Page.GoBack` → `PageContainer.PopPage` → попает `CurrentPage` (диалог), re-adds родителя в дерево, frees диалог. Два сценария:
   - «Остаться» (Back в диалоге): один `GoBack` в `OnBackPressed` → попается только диалог, родитель показывается снова. ✅
   - «Уйти» (Reset/Continue): колбэк родителя (`onReset`/`onContinue`) вызывает свой `GoBack` ВНУТРИ `Invoke()` → попает диалог (он CurrentPage). Затем `OnResetPressed`/`OnContinuePressed` вызывает `GoBack` ЕЩЁ РАЗ → попается уже родитель. **Оба `GoBack` обязательны** — убрать любой = диалог закрывается, родитель зависает.
3. **`PopPage` НЕ перезапускает `_Ready` родителя.** `RemoveChild` + `AddChild` одного узла без `QueueFree` не триггерят повторный `_Ready` (Godot вызывает `_Ready` только при первом входе в дерево). Значит подписки `Button.Pressed +=` не дублируются, draft-state сохраняется между показами. Это позволяет диалогу «остаться» корректно возвращать страницу с правками нетронутыми.
4. **`MainMenuPage.GoNext`/`GoBack` — `protected Action<IPage>` / `protected Action`.** `Setup(goBack, goNext)` вызывается `PageContainer.SetupPage` при каждом push, перевязывая замыкания только на новую страницу. Родитель хранит свои старые замыкания — его `GoBack` всегда попает себя (через `PopPage`, который берёт `CurrentPage`).

## Связанные страницы

- Архитектура клиент/сервер — [[client-server-subsystem-split]]
- Предыдущая миграция визуала (завершена) — [[main-menu-visual-migration]]


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

- time: 2026-08-07T13:56:15
  kind: evidence
  summary: "Phase 1 (Settings Foundation) завершена: GameSettings расширен 7 полями, [Category]+[Options] атрибуты, MenuGameSettings 11 свойств, OptionButton рендер, Convert round-trip, ApplyRuntimeSettings (fullscreen/size/volumes), 12 ключей локали, dotnet build 0 errors"
  source: "plans/phase-1-settings-foundation.md имплементация"
  affects: [main-menu-structure]

- time: 2026-08-07T13:58:08
  kind: decision
  summary: "Составлен детальный план Phase 2 (known-servers service) в plans/phase-2-known-servers-service.md. Скоуп без изменений: KnownServer record + KnownServersService (user://known-servers.json, GetAll/Add/Remove/Exists, без дедупликата). Документированное отклонение от общего плана: папка Scripts/Service/KnownServers/ + 2 файла (KnownServer.cs + KnownServersService.cs) вместо одного файла — зеркало ResumableGame/ (record + service рядом). Init только в ClientRootStarter (НЕ dedicated). GetAll возвращает живой список."
  source: "plans/phase-2-known-servers-service.md; анализ существующих сервисов-паттернов (ResumableGameService, GameSettingsService)"
  affects: [main-menu-structure]

- time: 2026-08-07T14:03:55
  kind: decision
  summary: "Phase 2 (Known Servers Service) завершена. Добавлен трекер прогресса по фазам."
  source: "Phase 2 имплементация"
  affects: [main-menu-structure]

- time: 2026-08-07T14:14:45
  kind: decision
  summary: "Составлен детальный план Phase 3 (Multiplayer хаб + серверные формы) в plans/phase-3-multiplayer-hub-and-server-forms.md. Скоуп без изменений: MultiplayerPage (хаб, 4 кнопки), CreateNewServerPage (порт логики HostPage), CreateSavedServerPage (форма + список сейвов с поиском), MainPage → один MultiplayerButton вместо Host+Connect. Документированные решения плана: (1) хаб использует neon-button template MainPage, формы — HostPage form shell; (2) Connect кнопка хаба временно → старый ConnectPage, Phase 4 заменит на ServerListPage и удалит ConnectPage; (3) HostPage (.cs+.tscn) удаляется В Phase 3 (замена в этой же фазе), ConnectPage — в Phase 4; (4) SaveName = LineEdit (не TextEdit), Port SpinBox min 1024; (5) CreateSavedServerPage re-populate списка на каждый TextChanged (case-insensitive contains), inline error через PrepareMessagePage если сейв не выбран; (6) PagesProvider rewiring в MainMenu.tscn через Godot editor (не hand-edit — uid/path/load_steps хрупкие). Реализация НЕ начата — делает другой агент."
  source: "plans/phase-3-multiplayer-hub-and-server-forms.md; анализ MainPage/HostPage/ConnectPage/SingleplayerPage/PagesProvider/Page system/MainMenu.tscn/Services.MainScene/Services.SaveLoad"
  affects: [main-menu-structure]

- time: 2026-08-07T14:37:23
  kind: decision
  summary: "Phase 3 (Multiplayer хаб + серверные формы) завершена. MultiplayerPage хаб создан, CreateNewServerPage + CreateSavedServerPage портированы из HostPage, MainPage → один MultiplayerButton, HostPage удалён, PagesProvider/MainMenu.tscn переподключены, 17 ключей локали добавлены, dotnet build 0 errors."
  source: "Phase 3 имплементация (plans/phase-3-multiplayer-hub-and-server-forms.md)"
  affects: [main-menu-structure]

- time: 2026-08-07T14:42:48
  kind: decision
  summary: "Составлен детальный план Phase 4 (ServerListPage + direct connect) в plans/phase-4-server-list-and-connect.md. Скоуп без изменений: ServerListPage (список известных серверов + inline add/remove + прямое подключение host:port), repoint MultiplayerPage.ConnectButton → ServerListPageScene, удаление ConnectPage. Документированные решения плана: (1) ServerListPage = form shell (PanelContainer), НЕ hub; list frame копируется node-for-node из CreateSavedServerPage.tscn; (2) add/remove/direct-connect на одной странице (не отдельная Add-страница); (3) порт = SpinBox (min 1 max 65535), host-only direct-connect → port=null (spec #12); (4) de-dup через KnownServers.Exists в странице (сервис НЕ дедуплицирует — решение Phase 2); (5) автодобавление в known при прямом подключении если нового; (6) ConnectionPageScene → переименован в ServerListPageScene (как Phase 3 сделала CreateNewServerPageScene); (7) CONNECT_MENU__* ключи остаются orphaned до Phase 8 (как HOST_MENU__* в Phase 3); (8) TODO (#16 gate Phase 5) маркер на OnConnectDirect. Реализация НЕ начата — делает другой агент."
  source: "plans/phase-4-server-list-and-connect.md; анализ ConnectPage.cs/.tscn, KnownServersService.cs, CreateSavedServerPage.cs/.tscn, PagesProvider.cs, MultiplayerPage.cs, MainMenu.tscn, Services.cs, MainSceneService.ConnectToMultiplayerGame"
  affects: [main-menu-structure]

- time: 2026-08-07T14:49:01
  kind: decision
  summary: "Phase 4 (ServerListPage + direct connect) завершена: ServerListPage создан (список известных серверов, inline add/remove, прямое подключение host:port), MultiplayerPage.ConnectButton repoint на ServerListPageScene, ConnectionPageScene → ServerListPageScene в PagesProvider, ConnectPage удалён, 14 SERVER_LIST_MENU__* ключей локали добавлены, dotnet build 0 errors"
  source: brain update-truth
  affects: [main-menu-structure]

- time: 2026-08-07T15:17:07
  kind: decision
  summary: "Phase 5 (First-run gate) завершена: PlayerSettingsPage создан (draft/preserved pattern, Player-category settings, Save sets PlayerSettingsAcknowledged=true + persist + continuation), TryStartGame(Action) helper в MainMenuPage, обёрнуты 4 точки старта (SingleplayerPage.OnStart, CreateNewServerPage.ParseAndStartServer, CreateSavedServerPage.OnCreate, ServerListPage.OnConnectDirect), PagesProvider + MainMenu.tscn подключены, 1 ключ локали PLAYER_SETTINGS_MENU__TITLE, dotnet build 0 errors"
  source: brain update-truth
  affects: [main-menu-structure]

- time: 2026-08-07T15:31:27
  kind: decision
  summary: "Составлен детальный план Phase 6 (custom ColorPickerPanel) в plans/phase-6-custom-colorpicker.md. Скоуп без изменений: ColorPickerPanel.cs (ColorPickerButton EditAlpha=false + editable hex LineEdit с валидацией Color.HtmlIsValid + preset-палитра 12 neon-swatch) — pure C# без .tscn; замена typeof(Color) записи в Configurators._configurators на new ColorPickerPanel(Handle.Value) + ColorChanged→Handle.Value. Документированные решения плана: (1) single-source-of-truth SetColor + _suppress guard против рекурсивного ColorChanged ping-pong; (2) TextChanged коммитит только при HtmlIsValid (не сбрасывает box mid-typing — иначе нельзя ввести #ff0), TextSubmitted/FocusExited snap-back к текущему цвету; (3) palette как HBox (row-efficiency) или VBox (general plan §6.1) — оба допустимы; (4) inline lambda в Configurators (4 строки) вместо отдельного ColorSettingConfigurator класса; (5) EditAlpha=false — PlayerColor RGB-only, hex контракт 6 цифр; (6) panel decoupled от Setting/Handle — принимает Color+palette в ctor, event ColorChanged наружу; (7) НЕТ .tscn, НЕТ PagesProvider/MainMenu.tscn wiring (code-only как SettingContainer), НЕТ locale keys; (8) PlayerSettingsPage Phase 5 и SettingsPage апгрейдятся автоматически через SettingContainer pipeline; (9) SettingContainer._Ready dispatch НЕ трогается — PlayerColor без [Options] падает в Configurators.GetFor(typeof(Color)). Реализация НЕ начата — делает другой агент."
  source: "plans/phase-6-custom-colorpicker.md; анализ SettingContainer.cs (Configurators._configurators Color entry, _Ready OptionsAttribute pre-check), Setting.cs (Value/Apply), MenuGameSettings.cs (PlayerColor [Category(Player)]), SettingsPage.cs (draft/preserved pattern), ColorJsonConverter.cs (ToHtml/FromHtml round-trip), Phase 5 plan (PlayerSettingsPage reuses SettingContainer)"
  affects: [main-menu-structure]

- time: 2026-08-07T16:34:48
  kind: decision
  summary: "Phase 6 (ColorPickerPanel) завершена. Палитра оказалась GridContainer 6 колонок (не HBox — 12 swatch не помещались в строку SettingContainer). Урок layout: Control-наследник не агрегирует min-size детей, SettingContainer._Ready перетирал высоту input на 20px — фикс: CustomMinimumSize.Y=input.CustomMinimumSize.Y (уважать собственную min-height контрола) + явная min-height в самой панели (~92px)."
  source: "Phase 6 имплементация (plans/phase-6-custom-colorpicker.md); отладка layout overflow палитры за границы SettingContainer"
  affects: [main-menu-structure]

- time: 2026-08-07T16:47:18
  kind: decision
  summary: "Составлен детальный план Phase 7 (Settings хаб + 5 категорий + ConfirmDialog) в plans/phase-7-settings-hub-and-categories.md. Ключевые решения: (1) SettingsHubPage = неон-кнопки по образцу MultiplayerPage, 5 категорий + Back; (2) один обобщённый SettingsCategoryPage(category,titleKey) для всех 5 категорий — НЕ 5 отдельных классов; Controls категория пустая (нет [Category(\"Controls\")] полей), страница должна работать с пустым списком; (3) dirty-проверка через _draftSettings.Serialize() != _preservedSettings.Serialize() (JSON-строка, ColorJsonConverter детерминирован); (4) ConfirmDialogPage = обобщение MessagePage (3 кнопки Reset/Back/Continue, колбэки onReset/onContinue/onBack); (5) PlayerSettingsPage (Phase 5 gate) ОСТАЁТСЯ как есть — НЕ перенаправляется в хаб; хаб-категория Player = отдельный plain-save путь через SettingsCategoryPage(\"Player\"). Два класса сосуществуют (вариант 'разделить на 2 класса' из допущений общего плана); (6) ConfirmDialog callback/pop ordering: колбэк категории вызывает GoBack (попает dialog), затем OnResetPressed/OnContinuePressed вызывает GoBack снова (попает category) — оба вызова обязательны; (7) OnSave ОБЯЗАТЕЛЬНО обновляет _preservedSettings после ApplyAndSaveSettings, иначе Save→Back повторно триггерит dirty-диалог; (8) удаляются SettingsPage.cs+tscn и PagesProvider.SettingsPageScene; MainPage.SettingsButton → PreparePage(SettingsHubPageScene). 15 новых ключей локали (SETTINGS_HUB__*, CONFIRM_DIALOG__*). Файлы (новые ≈6): SettingsHubPage(.cs+.tscn), SettingsCategoryPage(.cs+.tscn), ConfirmDialogPage(.cs+.tscn). Удаляемые (≈2): SettingsPage.cs+tscn."
  source: "plans/phase-7-settings-hub-and-categories.md; анализ существующих MultiplayerPage/SettingsPage/MessagePage как шаблонов, MenuGameSettings [Category] полей (Controls отсутствует), GameSettingsBase.GetVisibleSettings(string)+Serialize(), PageContainer.PopPage semantics"
  affects: [main-menu-structure]

- time: 2026-08-07T16:53:48
  kind: decision
  summary: "Phase 7 (Settings хаб) завершена"
  source: "plans/phase-7-settings-hub-and-categories.md имплементация"
  affects: [main-menu-structure]

- time: 2026-08-07T16:53:56
  kind: evidence
  summary: "Phase 7 (Settings хаб + 5 категорий + ConfirmDialog) реализована: SettingsHubPage (5 неон-кнопок Player/Controls/Interface/Graphics/Audio + Back, клон MultiplayerPage), обобщённый SettingsCategoryPage(category,titleKey) рендерит категорию через SettingContainer (Controls пустой — работает), ConfirmDialogPage (Reset/Back/Continue + onReset/onContinue/onBack колбэки, клон MessagePage). Dirty = Serialize() != Serialize(). OnSave обновляет _preservedSettings post-save (Save→Back без re-prompt). MainMenu.tscn: удалён SettingsPage ext_resource + SettingsPageScene assignment, добавлены SettingsHub/Category/ConfirmDialog slots (load_steps 18→20). Удалены SettingsPage.cs+tscn. MainPage.SettingsButton → SettingsHubPageScene. 15 ключей локали (SETTINGS_HUB__*, CONFIRM_DIALOG__*) в pot/en/ru. dotnet build 0 errors. PlayerSettingsPage (Phase 5 gate) НЕ тронут — сосуществует с хаб-категорией Player. Уникальные uid не проставлены в новых .tscn — Godot назначит при импорте (рекомендуется открыть MainMenu.tscn в редакторе для верификации slots + ручного теста 7.6c)."
  source: "plans/phase-7-settings-hub-and-categories.md имплементация; dotnet build 0 errors"
  affects: [main-menu-structure]

- time: 2026-08-07T17:17:53
  kind: decision
  summary: "Добавлен раздел Pages-system паттерны (configure-before-_Ready, callback/pop ordering для confirm-dialog, PopPage не перезапускает _Ready, GoNext/GoBack замыкания)"
  source: "Phase 7 имплементация (plans/phase-7-settings-hub-and-categories.md); анализ Scenes/Screen/NewMenu/PagesSystem/"
  affects: [main-menu-structure]
