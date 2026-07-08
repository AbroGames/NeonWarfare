# Поток запуска

[← README проекта](../../README.md)

Запуск разнесён на два независимых уровня, и путать их нельзя:

1. **RootStarter** — уровень **процесса**. Один на всё время жизни приложения. Решает, кто мы:
   клиент или выделенный сервер.
2. **GameStarter** — уровень **игровой сессии**. Создаётся заново на каждый вход в игру. Решает, как
   поднять конкретную сессию: поднять ENet-сервер, подключиться ENet-клиентом или не трогать сеть.

Аргументы командной строки разбираются **только** в RootStarter-ах, дальше едут обычными параметрами
(см. [Параметры командной строки](../Cli-args.md)).

## Уровень 1: RootStarter

`Root._Ready()` вызывает `RootStarterManager.ChooseStarter()`, которая проверяет, есть ли `"--server"`
в `OS.GetCmdlineArgs()`: если нет — выбирается `ClientRootStarter`, если есть —
`DedicatedServerRootStarter`.

Далее в обоих случаях у выбранного стартера (`ClientRootStarter` / `DedicatedServerRootStarter`)
вызываются два метода:

* `Init()` — сперва общий `BaseRootStarter.Init()`: обработчик исключений, кэши, `LoadingScreenService`,
  `I18N`. Затем специфичное для конкретного `*RootStarter.Init()`: `Net.Init()`, настройки, локаль,
  автомасштаб UI.
* `Start()` — запуск нужного сценария через `Services.MainScene.*`.

`RootData` (контейнеры, `RootPackedScenes`, `SceneTree`) прокидывается в стартер
параметром, глобального доступа к `Root` у стартера нет.

### Общая часть: `BaseRootStarter`

`Init()` одинаков для обеих ролей и идёт строго в этом порядке:

1. `Di.Process(this)`.
2. Разбор `CommonArgs`, включение дублирования логов в консоль Godot по `--godot-log-push`.
3. `Services.ExceptionHandler` — глобальный обработчик необработанных исключений.
4. Логирование полученных аргументов командной строки.
5. `Services.AssemblyCache` + `Services.TypesMapping` — кэш сборки и маппинг типов. Без этого шага не
   работают сканирования по сборке (например, автоподбор `ICommandProcessor`).
6. `Services.LoadingScreen.Init(...)`, `Services.MainScene.Init(...)` — сервисам отдаются контейнеры
   `Root` и прототипы сцен.
7. `Services.I18N.Init(sceneTree)`.

`Start()` в базе только пишет лог — весь сценарий запуска в наследниках.

### `ClientRootStarter`

Выбирается, когда `--server` **не** передан. Это обычный игровой процесс: и главное меню, и одиночная
игра, и подключение к чужому серверу, и хост «изнутри клиента».

`Init()` — после `base.Init()`:

| Шаг | Зачем |
|---|---|
| `ClientArgs.GetFromCmd(...)` | Разбор клиентских флагов |
| `Services.Net.Init(false)` | Процесс **не** выделенный сервер → `Net.IsClient()` всегда `true` |
| `Services.AutoScaling.Init(...)` | Автомасштаб UI по `Consts.AutoScalingSettings` |
| `Services.LastGame.Init()` | Чтение `resume-game.json` для кнопки «Продолжить» |
| `Services.GameSettings.Init()` | Чтение `game-settings.json` |
| `--nick` / `--uid` | Временный оверрайд ника и UID, **без записи** в файл настроек |
| `Services.I18N.SetCurrentLocale(...)` | Локаль из `GameSettings` |
| `Services.LoadingScreen.SetLoadingScreen(Loading)` | Первый показ экрана загрузки |

`Start()` — выбор сценария по флагам, ровно один из трёх:

| Условие | Действие |
|---|---|
| `--auto-start` | `MainScene.StartSingleplayerGame(...)`. Имя сейва — из `--auto-start-savefile`, а если флага нет — сгенерированное `SaveLoad.GenNewSaveFileName()` |
| `--auto-connect` | `MainScene.ConnectToMultiplayerGame(--auto-connect-ip, --auto-connect-port)` |
| иначе | `MainScene.StartMainMenu()` + `LoadingScreen.Clear()` |

### `DedicatedServerRootStarter`

Выбирается по флагу `--server`. Головного игрока в этом процессе нет.

`Init()` — после `base.Init()`:

| Шаг | Зачем |
|---|---|
| `DedicatedServerArgs.GetFromCmd(...)` | Разбор серверных флагов |
| `Services.Net.Init(true)` | Процесс выделенный сервер → `Net.IsClient()` всегда `false` |
| `Services.LastGame.Init()` | Чтение `resume-game.json` |
| `Services.DedicatedServerSettings.Init()` | Чтение `dedicated-server-settings.json` |
| `Services.I18N.SetCurrentLocale(...)` | Локаль из `DedicatedServerSettings`, **после** их загрузки |
| Заголовок окна | Префикс `[SERVER]`, чтобы не путать окна при локальном тесте |

Отличия от клиента, о которых легко забыть: выделенный сервер **не** инициализирует
`Services.GameSettings` и `Services.AutoScaling` и **не** показывает экран загрузки — ему нечего
показывать игроку.

`Start()` — единственный сценарий: `MainScene.HostMultiplayerGameAsDedicatedServer(...)` с именем
сейва из `--savefile` (или сгенерированным), портом, админским UID, `--parent-pid`, `--no-hud` и
`--world-render`.

## Уровень 2: GameStarter

`MainSceneService` создаёт сцену `Game`, кладёт её в `MainSceneContainer` и передаёт ей **стартер
игры** — объект, который знает, как именно поднять эту сессию:

`Game.Init(BaseGameStarter starter)` вызывает `starter.Init(game)`, который последовательно
выполняет:

* `game.AddNetwork()` — создаёт `Network` (ENet + `SceneMultiplayer`).
* `game.AddWorld()` — создаёт `World`.
* `game.AddHud()` / `game.AddServerHud()`.
* `ServerStartWorld()` — на сервере: `StartNewGame()` или `LoadGame()`.
* `ClientStartWorld()` — на клиенте: `StartSyncWithServer()`.

Точки входа в `Services.MainScene`, стартеры, которые за ними стоят, и когда каждый из них
используется:

| Стартер | Метод `MainSceneService` | Сеть / хост | Когда используется |
|---|---|---|---|
| `SingleplayerGameStarter` | `StartSingleplayerGame(saveFileName)` | нет, `Network` не создаётся; процесс сам себе сервер | Одиночная игра из меню, `--auto-start` (+ `--auto-start-savefile`) |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: false)` | ENet-сервер в этом же процессе | Хост «изнутри клиента» |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsDedicatedServer(...)` | ENet-сервер в этом же процессе | Выделенный сервер (`--server`) |
| `ConnectToMultiplayerGameStarter` | `ConnectToMultiplayerGame(host, port)` | ENet-клиент, хост — удалённый процесс | Подключение к серверу из меню, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: true)` | ENet-клиент + дочерний процесс-сервер | Хост с вынесенным сервером: поднимает второй процесс ОС |

Кто из этих режимов и когда возвращает `true`/`false` из `Net.IsServer()` / `Net.IsClient()` — в
[Сети](Networking.md#роли-процесса-isserver--isclient).

### Общая часть: `BaseGameStarter`

Четыре защищённых метода, которыми пользуются все стартеры:

* **`ServerStartWorld(world, saveFileName, adminUid)`** — серверный старт мира. `saveFileName`
  обязателен (`null` → `ArgumentNullException`). Если файла сейва нет — `StartNewGame(...)`, если
  есть — `LoadGame(...)`. `LoadException` при загрузке не роняет процесс: на клиенте она уводит в
  меню с текстом ошибки.
* **`ClientStartWorld(world)`** — клиентский старт: `ClientStartStopService.StartSyncWithServer(...)`,
  то есть хендшейк из [Сеть](Networking.md).
* **`SetLastGame(...)` / `AddLastGameUpdaterToSaveEvent(...)`** — запись сессии в `resume-game.json`.
  Второй метод подписывается на `SaveSuccessServerEvent` и обновляет запись именем нового сейва, так
  что «Продолжить» после ручного сохранения ведёт на актуальный файл.
* **`GoToMenuAndShowError(message)` / `GoToMenu()`** — возврат в меню. Оба начинаются с проверки
  `Net.IsClient()`: на выделенном сервере меню нет.

### 1. `SingleplayerGameStarter`

Одиночная игра. **Сеть не создаётся вообще** — `AddNetwork()` не вызывается, `Network` в дереве нет.
Процесс сам себе авторитет, `Net.IsServer()` возвращает `true`.

1. Экран загрузки `Loading`.
2. `AddWorld()`, `AddHud()`.
3. `resume-game.json` — режим «одиночная игра» + подписка на успешное сохранение.
4. `ServerStartWorld(...)`, где админский UID — `GameSettings.PlayerUid`: игрок админ в собственной игре.
5. `ClientStartWorld(...)` — тот же хендшейк, что и в сетевой игре, просто локальный.

### 2. `HostMultiplayerGameStarter`

Поднимает ENet-сервер **в этом же процессе**. Используется в двух совершенно разных случаях: хост
«изнутри клиента» (`HostMultiplayerGameAsClient`) и выделенный сервер
(`HostMultiplayerGameAsDedicatedServer`). Различия задаются флагами конструктора, а не отдельным
классом.

Параметры конструктора: `saveFileName`, `port`, `adminUid`, `parentPid`, `serverHudRender`,
`worldRender`, `mustSetLastGame`, `startedAsDedicated`.

1. Экран загрузки `Loading`.
2. Если передан `parentPid` — на `Game` вешается `ProcessDeadChecker` (нода из KludgeBox), который
   следит за родительским процессом и вызывает `MainScene.Shutdown()`, когда тот умирает. Так
   дочерний сервер не остаётся висеть после закрытия клиента (см. [Завершение работы](Shutdown.md)).
3. `AddNetwork()`, `AddWorld()`, `Net.DoClient(() => AddHud())` — HUD только там, где есть игрок.
4. `serverHudRender` → `AddServerHud()`; `worldRender == false` → `world.SetVisible(false)`. Это и
   есть флаги `--no-hud` и `--world-render`: выделенный сервер по умолчанию рисует консоль, а не мир.
5. `mustSetLastGame` → запись в `resume-game.json`. У сервера, запущенного из консоли, её нет —
   писать «продолжить» некому.
6. `network.HostServer(port ?? 25566, true)`. Ошибка (например, занятый порт) → на клиенте уход в
   меню с текстом, дальше стартер не идёт.
7. `ServerStartWorld(...)` → `network.OpenServer()` → `Net.DoClient(() => ClientStartWorld(...))`.

> [!IMPORTANT]
> Порядок шага 7 обязателен: сервер открывается для входящих подключений **только после** того, как
> мир поднят. Иначе клиент успеет постучаться в ещё не существующий мир.

### 3. `ConnectToMultiplayerGameStarter`

Подключение к чужому серверу. Единственный режим, в котором `Net.IsServer()` возвращает `false`.

Параметры: `host`, `port`, `mustSetLastGame`.

1. Экран загрузки `Connecting` — с кнопкой отмены, по которой вызывается `GoToMenu()`.
2. `AddNetwork()`, `AddWorld()`, `AddHud()`. Мир создаётся сразу, но **пустым**: его наполнит снимок
   от сервера.
3. Подписки на события `MultiplayerApi`:
   * `ConnectedToServer` → `ClientStartWorld(...)`;
   * `ConnectionFailed` → в меню с «Connection to the server failed» (сервер не ответил за таймаут);
   * `ServerDisconnected` → в меню с «Server disconnected» (может прилететь и через часы игры).
4. `mustSetLastGame` → запись «подключение к серверу» в `resume-game.json`.
5. `network.ConnectToServer(host ?? 127.0.0.1, port ?? 25566)`. Синхронная ошибка обрабатывается тем
   же `ConnectionFailedEvent`.

> [!IMPORTANT]
> `ClientStartWorld` вызывается **по событию**, а не сразу: на момент `Init()` соединения ещё нет.
> Обработчик `ConnectedToServer` написан локальной функцией и отписывается от события первым же
> срабатыванием — иначе течёт `SynchronizerService` при возврате в меню
> (см. [Соглашения по написанию кода](../Code-style.md)).

### 4. `HostDedicatedServerAndConnectGameStarter`

Хост с вынесенным сервером: поднимает **второй процесс ОС** и подключается к нему как обычный клиент.
Наследник `ConnectToMultiplayerGameStarter(Localhost, port, mustSetLastGame: false)`.

1. `Services.Process.StartNewDedicatedServerApplication(...)` запускает новый процесс с `--server`,
   передавая ему `--port`, `--savefile`, `--admin` и **`--parent-pid` с PID текущего процесса**.
   `--headless` ставится, когда окно сервера не запрошено; дублирование логов в консоль Godot
   выделенному серверу не передаётся никогда.
2. На `Game` вешается `ProcessShutdowner` (нода из KludgeBox) с PID сервера: при уничтожении `Game`
   дочерний процесс будет убит.
3. `base.Init(game)` — дальше это обычное подключение к `127.0.0.1`.
4. Запись в `resume-game.json` — вручную, **после** `base.Init`, как «свой сервер». Именно поэтому в
   конструктор базы передан `mustSetLastGame: false`: иначе база записала бы режим «подключение к
   чужому серверу», и кнопка «Продолжить» перестала бы поднимать сервер.

Живучесть пары процессов держится на двух нодах сразу: `ProcessShutdowner` убивает сервер при
нормальном закрытии клиента, `ProcessDeadChecker` на стороне сервера — страховка на случай, когда
клиент умер аварийно и убить никого не успел.
