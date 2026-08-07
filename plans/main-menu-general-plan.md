# MainMenu General Plan — Epic #12 (Creation of MainMenu)

Скоуп: полная реструктуризация меню по спеке issue #12 + сервер-лист (часть #94) + 5 категорий настроек + first-run gate #16.

## Контекст (что есть)

**Архитектура (не трогать):** Page-система `Page`/`PageContainer`/`IPage` (push/pop), `MainMenu` → `PageContainer` + `PagesProvider`. Страница = папка `Pages/<Name>/` с `.cs` (`MainMenuPage` + `[Child]` инъекция через `Di.Process`) + `.tscn`. Навигация: `GoNext(PagesProvider.PreparePage(scene))` / `GoBack()`. DI-паттерн: `[Export][NotNull]` + `Di.Process(this)` (НЕ `NotNullChecker`, его нет).

**Visual:** неон-cyan, фон = 3D-коридор в `MainMenu.tscn` (SubViewport). Кнопки MainPage = `StyleBoxEmpty` normal + cyan `GradientTexture1D` hover, Play-Bold. Страницы (Singleplayer/Host/Connect) = PanelContainer с margin. Каждый новый экран — в этом же стиле.

**Существующие страницы:** MainPage (6 кнопок, нет Multiplayer), SingleplayerPage (TabContainer new+load), HostPage (единая форма Port+SaveName+IsDedicated), ConnectPage (разделённые Host + Port поля), SettingsPage (плоский список 4 настроек), MessagePage (только OK).

**Сервисы:** `Services.MainScene.StartSingleplayerGame/HostMultiplayerGameAsClient/ConnectToMultiplayerGame/Shutdown`, `Services.SaveLoad.GetAllSaveFiles/GenNewSaveFileName`, `Services.LastGame` (Resume), `Services.MenuGameSettings.GetSettings/ApplyAndSaveSettings`, `Services.GameSettings.GetSettings/SetSettings` (персист в `user://game-settings.json`).

**SettingsSystem:** reflection-based, `MenuGameSettings` (partial×2), атрибуты `[Name]/[Hint]/[Range]/[Step]/[Hide]`, рендер через `SettingContainer` + словарь `Configurators` (bool→CheckBox, числа→HSlider/SpinBox, string→LineEdit, Color→ColorPickerButton). Сериализация JSON с `ColorJsonConverter`. Категоризации НЕТ. Dropdown-опций НЕТ.

**Локаль:** `Assets/Locales/` (`messages.pot`, `en.po`, `ru.po`). Конвенция `SECTION__KEY` (внимание: `SETTING_MENU__` единственное число, НЕ `SETTINGS_MENU__`). Все новые ключи → во все 3 файла. `Tr()` неявно в лейблах, явно в C# (`Tr("...")`).

**TODO в scope:** 0.

## Главные изменения структуры (всё по спеке #12)

```
MainPage
├─ Resume (если есть LastGame)              [уже есть, оставить]
├─ Одиночная игра (Singleplayer)            [есть → доработать]
│   ├─ Новая игра → PlayerSettings gate(#16) → SingleplayerPage
│   └─ Загрузить сохранение → PlayerSettings gate(#16) → SingleplayerPage(load-tab)
├─ Игра по сети (Multiplayer)               [НОВОЕ — хаб-страница]
│   ├─ Создать новую игру → gate(#16) → CreateNewServerPage
│   ├─ Создать из сохранения → gate(#16) → CreateSavedServerPage
│   └─ Подключиться к серверу → gate(#16) → ServerListPage
├─ Настройки → SettingsHubPage              [НОВОЕ — хаб категорий]
│   ├─ Игрок → PlayerSettingsPage
│   ├─ Управление → ControlsSettingsPage
│   ├─ Интерфейс → InterfaceSettingsPage
│   ├─ Графика → GraphicsSettingsPage
│   └─ Аудио → AudioSettingsPage
├─ Смена языка → LanguageSelectionPage      [есть]
└─ Выход                                    [есть]
```

PlayerSettings gate (#16): перед каждым запуском игры (single/multi new/connect) проверять флаг `PlayerSettingsAcknowledged` в `GameSettings`. Если false → push `PlayerSettingsPage` (ник+цвет, кнопка «Сохранить» снимает флаг). После save → запуск игры.

## Фазы (после каждой — `dotnet build`, 0 ошибок)

### Фаза 1 — Фундамент: SettingsSystem + persistence

**1.1 Расширить `GameSettings`** (`Scripts/Service/Settings/GameSettings.cs`): добавить поля
- `PlayerSettingsAcknowledged` (bool, default false) — для gate #16
- `MasterVolume`, `SoundsVolume`, `MusicVolume` (int, default 100)
- `Fullscreen` (bool, default false)
- `Resolution` (string, default `"1280x720"`)
- `InterfaceSize` (string, default `"Medium"`)

В `GetDefault()` проставить. JSON-сериализация record — автоматически.

**1.2 Расширить `MenuGameSettings`** (partial в `SettingsSystem/`): добавить те же поля с `[Name]/[Hint]` (ключи `SETTING_MENU__*`). `Validate()` — null-коалесы для новых строк.

**1.3 Добавить категоризацию в SettingsSystem:**
- Новый атрибут `[Category(string)]` в `SettingAttributes.cs`
- В `GameSettingsBase.cs` (partial MenuGameSettings) добавить `GetVisibleSettings(string category)` / `SetVisibleSettings(IReadOnlyList<Setting>, string)` — фильтр `VisibleAccessors` по `[Category]`. Текущий безаргументный `GetVisibleSettings()` оставить как «все видимые» (обратная совместимость).
- Пометить существующие: PlayerName/PlayerColor/PlayerUid/PlayerSettingsAcknowledged `[Category("Player")]`, AutoSaveEnabled `[Category("Player")]` (или скрыть). Новые поля — по категориям.

**1.4 Добавить `OptionSetting`/dropdown:** новый атрибут `[Options(string[] keys)]` (ключи перевода) в `SettingAttributes.cs`; новый configurator в `Configurators._configurators` — но `Options` это не тип, поэтому вместо типа-ключа регистрировать по наличию атрибута. Решение: в `SettingContainer._Ready` проверять `Handle.Member.GetAttribute<OptionsAttribute>()` первым и строить `OptionButton` (Godot dropdown) из `Tr(key)` для каждого, иначе fallback на тип-конфигуратор. Применить к `Resolution` и `InterfaceSize`.

**1.5 `MenuGameSettingsService.Convert`** — пробросить новые поля в обе стороны. `ApplySettings` — применить `DisplayServer.WindowSetMode` (Fullscreen), `AudioServer` громкости, locale.

**1.6 Верификация:** build.

### Фаза 2 — Сервисы: known-servers (#94 часть)

**2.1 `Scripts/Service/KnownServersService.cs`** (новый): record `KnownServer(string Host, int? Port, string Label)`, список в `user://known-servers.json` (JSON, mirror `GameSettingsService` паттерн). Методы: `GetAll()`, `Add(KnownServer)`, `Remove(KnownServer)`, `Exists(host,port)`. Без дедупликата host:port.

**2.2 Зарегистрировать** в `Services.cs`: `KnownServers`.

**2.3 Верификация:** build.

### Фаза 3 — Х страницы: Multiplayer хаб + серверные формы

**3.1 `MultiplayerPage`** (новая папка `Pages/Multiplayer/`): 4 кнопки (Create New / Create from Save / Connect / Back), неон-стиль как MainPage buttons. GoNext на соответствующие.

**3.2 MainPage.tscn + .cs:** заменить `StartSingleplayerButton`→Singleplayer (есть, но переименовать в логике), `CreateServerButton`+`ConnectToServerButton`→один `MultiplayerButton`→MultiplayerPage. Оставить Resume/Settings/Language/Quit. `[Child] MultiplayerButton`.

**3.3 `CreateNewServerPage`** (новая папка `Pages/CreateNewServer/`): по спеке — Port (SpinBox 1024-65535, default 25566), SaveName (LineEdit, default `GenNewSaveFileName()`), CheckButton «Launch server in new window» (=IsDedicated), Create/Back. Логика = текущий `HostPage.ParseAndStartServer`. Перенести.

**3.4 `CreateSavedServerPage`** (новая папка `Pages/CreateSavedServer/`): Port, список сохранений (ScrollContainer VBox + LineEdit-поиск с динамическим фильтром по `TextChanged`), CheckButton dedicated, Create/Back. Фильтр: перепопуляция списка при каждом изменении, case-insensitive contains по FileName. Создание игры: `HostMultiplayerGameAsClient(saveFileName, port, isDedicated)`.

**3.5 SingleplayerPage:** оставить TabContainer, но обернуть кнопки стартов в gate #16 (см. Фаза 5). Убрать из MainPage прямой путь.

**3.6 `PagesProvider`:** добавить `[Export][NotNull]` для новых сцен + `PreparePage`.

**3.7 Верификация:** build.

### Фаза 4 — ServerList + Connect (замена ConnectPage)

**4.1 `ServerListPage`** (новая папка `Pages/ServerList/`):
- Левая/верхняя часть: VBox со списком известных серверов из `Services.KnownServers.GetAll()` (Label `host:port` + Label `label`), выделение кликом.
- Кнопки: Add (открывает inline-форму host+port+label или отдельную страницу), Remove, Connect.
- Поле прямого подключения «host:port» (по спеке #12: без порта → null).
- Add: валидация, `Services.KnownServers.Add`.
- Remove: `Services.KnownServers.Remove` выбранного.
- Connect: `Services.MainScene.ConnectToMultiplayerGame(host, port)` + автодобавление в known если нового.
- Back.

**4.2 Удалить старый `ConnectPage`** (`.cs`+`.tscn`), убрать из `PagesProvider`.

**4.3 Верификация:** build.

### Фаза 5 — First-run gate (#16)

**5.1 PlayerSettingsPage как modal-style page** (новая папка `Pages/PlayerSettings/`): ник (LineEdit) + цвет (кастомный ColorPicker из Фаза 6) + Save/Cancel. Save → `ApplyAndSaveSettings`, `GameSettings.PlayerSettingsAcknowledged = true`, затем continuation-action (запуск игры). Cancel → обратно, без запуска.

**5.2 Gate helper:** статический метод в `PagesProvider` или новом `PlayerSettingsGate` — `TryStartGame(Action startAction)`: если `!GameSettings.GetSettings().PlayerSettingsAcknowledged` → `GoNext(PreparePlayerSettingsPage(startAction))`, иначе `startAction()`. PlayerSettingsPage хранит continuation в поле, вызывает после save.

**5.3 Обернуть** все 3 точки старта игры (SingleplayerPage.OnStart, CreateNewServer/CreateSavedServer.OnCreate, ServerListPage.OnConnect) в `TryStartGame`.

**5.4 Верификация:** build.

### Фаза 6 — Кастомный ColorPicker

**6.1 `Scenes/Screen/NewMenu/SettingsSystem/ColorPickerPanel.cs`** (новый Control, программно как `SettingContainer`): HBox с `ColorPickerButton` + hex `LineEdit` (двусторонняя синхронизация, валидация `Color.HtmlIsValid`) + VBox preset-кнопок (предгенерённые cyan/teal/фиолетовый/жёлтый/красный/зелёный/белый/чёрный, как «палитра»). `ColorChanged` event наружу.

**6.2** Заменить `Color`-configurator в `Configurators` на использование `ColorPickerPanel`.

**6.3 Верификация:** build.

### Фаза 7 — Settings хаб + 5 категорий + Unsaved-changes dialog

**7.1 `SettingsHubPage`** (новая папка `Pages/SettingsHub/`): 5 кнопок-категорий + Back, неон-стиль.

**7.2 Категории-страницы** (новые папки `Pages/Settings/<Category>/`): каждая = `SettingsCategoryPage` обобщённая: `Category` string, рендерит `SettingsContainer` через `_draftSettings.GetVisibleSettings(Category)`. Save/Cancel. Back → если есть dirty (сравнение draft vs preserved) → ConfirmDialog.
- PlayerSettingsPage из Фаза 5 — становится категорией Player (reuse через тот же компонент, но без gate-continuation когда открыт из Settings).
- ControlsSettingsPage — категория Controls (пока пустая: только Back + unsaved-проверка no-op).
- InterfaceSettingsPage — категория Interface (InterfaceSize dropdown).
- GraphicsSettingsPage — категория Graphics (Resolution dropdown, Fullscreen checkbox).
- AudioSettingsPage — категория Audio (3 слайдера 0-100).

**7.3 `SettingsPage` (старый)** → удалить, заменить на `SettingsHubPage`. MainPage.SettingsButton → SettingsHubPage.

**7.4 `ConfirmDialogPage`** (новая папка `Pages/ConfirmDialog/`): сообщение + 3 кнопки (Reset changes / Back / Continue). Обобщение `MessagePage` — принимает message + `Action onReset/onContinue`. Используется из `SettingsCategoryPage.Back` при dirty.

**7.5** Dirty-проверка: сравнить `_draftSettings.Serialize()` vs `_preservedSettings.Serialize()` (JSON-строка). Простой и надёжный.

**7.6 Верификация:** build.

### Фаза 8 — Локаль + финальная полировка

**8.1** Добавить все новые ключи в `Assets/Locales/messages.pot` + `en.po` + `ru.po`: `MULTIPLAYER_MENU__*`, `CREATE_SERVER_MENU__*`, `CREATE_SAVED_SERVER_MENU__*`, `SERVER_LIST_MENU__*`, `PLAYER_SETTINGS_MENU__*`, `SETTINGS_HUB__*`, `SETTING_MENU__MASTER_VOLUME/SOUNDS_VOLUME/MUSIC_VOLUME/FULLSCREEN/RESOLUTION/INTERFACE_SIZE`, `CONFIRM_DIALOG__*`, `SETTING_MENU__PLAYER_SETTINGS_ACKNOWLEDGED` (или Hide). Переводы RU + EN.

**8.2** Прогнать `dotnet build`, исправить 0 ошибок.

**8.3 Ручная верификация в Godot-редакторе** (пометить в отчёте, не автоматизировать): открыть MainMenu.tscn, проверить рендер всех страниц, навигацию, gate #16, флоу single/multi/connect, настройки.

## Допущения / риски

- **`Configurators` тип-словарь vs `OptionsAttribute`:** Options не тип, поэтому регистрация по типу не работает. Решение в 1.4 — pre-check атрибута в `SettingContainer._Ready` перед тип-конфигуратором.
- **GameSettings как record + новый сервис KnownServers:** оба персиста в `user://`. Record immutable — обновление через `with`.
- **PlayerSettingsPage dual-role** (gate-modal + settings-category): один компонент, поведение зависит от наличия continuation-action. Если усложнит — разделить на 2 класса (PlayerSettingsPage-gate + PlayerSettingsCategoryPage), обе рендерят через `SettingsCategoryPage("Player")`. Решу при реализации; предпочту единый компонент с nullable continuation.
- **Visual detail (отступы, размеры кнопок):** копировать из существующих tscn (HostPage/ConnectPage — PanelContainer с margin 20; MainPage — StyleBoxEmpty+cyan gradient).
- **MainPage Resume:** спека #12 не упоминает, но уже работает и полезно. Оставляю.
- **AutoScale (#137, открытый):** не в этом эпике. InterfaceSize настройка добавляется, но реальный scaling-mechanism — отдельно.

## Файлы (краткая сводка)

**Новые (≈22):** MultiplayerPage(.cs+.tscn), CreateNewServerPage(×2), CreateSavedServerPage(×2), ServerListPage(×2), PlayerSettingsPage(×2), SettingsHubPage(×2), ControlsSettingsPage(×2), InterfaceSettingsPage(×2), GraphicsSettingsPage(×2), AudioSettingsPage(×2), ConfirmDialogPage(×2), ColorPickerPanel(.cs), KnownServersService(.cs), `CategoryAttribute`/`OptionsAttribute` (в SettingAttributes.cs — правка).

**Изменяемые (≈9):** GameSettings.cs, MenuGameSettings.cs (×2 partial), GameSettingsBase.cs, MenuGameSettingsService.cs, SettingContainer.cs, PagesProvider.cs, MainPage.cs+MainPage.tscn, Services.cs, 3 файла локали.

**Удаляемые (≈4):** ConnectPage.cs+ConnectPage.tscn, SettingsPage.cs+SettingsPage.tscn.

## Порядок исполнения

Фазы 1→8 последовательно, build после каждой. Фаза 1 (фундамент) блокирует 5/7. Если build ломается — фикс там же, дальше не двигаюсь. После Фазы 8 — запись в brain (create-page `main-menu-structure` + decision timeline) и отчёт по ручной верификации.
