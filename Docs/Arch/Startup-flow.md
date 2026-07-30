# Startup flow

[← Project README](../../README.md)

The startup is split into two independent levels, and they must not be confused:

1. **RootStarter** — the **process** level. One for the whole lifetime of the application. It decides
   who we are: a client or a dedicated server.
2. **GameStarter** — the **game session** level. Created anew on every entry into the game. It
   decides how to bring up a specific session: bring up an ENet server, connect as an ENet client, or
   not touch the network at all.

The command-line arguments are parsed **only** in the RootStarters, and from there travel on as
ordinary parameters (see [Command-line arguments](../Cli-args.md)).

## Level 1: RootStarter

`Root._Ready()` calls `RootStarterManager.ChooseStarter()`, which checks whether `"--server"` is
present in `OS.GetCmdlineArgs()`: if it is not — `ClientRootStarter` is chosen, if it is —
`DedicatedServerRootStarter`.

After that, in both cases two methods are called on the chosen starter (`ClientRootStarter` /
`DedicatedServerRootStarter`):

* `Init()` — first the common `BaseRootStarter.Init()`: the exception handler, the caches,
  `LoadingScreenService`, `I18N`. Then whatever is specific to the particular `*RootStarter.Init()`:
  `Net.Init()`, the settings, the locale, UI auto-scaling.
* `Start()` — launching the required scenario through `Services.MainScene.*`.

`RootData` (the containers, `RootPackedScenes`, `SceneTree`) is passed into the starter as a
parameter; the starter has no global access to `Root`.

### The common part: `BaseRootStarter`

`Init()` is the same for both roles and goes strictly in this order:

1. `Di.Process(this)`.
2. Parsing `CommonArgs`, enabling log mirroring into the Godot console per `--godot-log-push`.
3. `Services.ExceptionHandler` — the global handler for unhandled exceptions.
4. Logging the command-line arguments that were received.
5. `Services.AssemblyCache` + `Services.TypesMapping` — the assembly cache and the type mapping.
   Without this step the assembly-wide scans do not work (for example, the automatic pickup of
   `ICommandProcessor`).
6. `Services.LoadingScreen.Init(...)`, `Services.MainScene.Init(...)` — the services are handed the
   `Root` containers and the scene prototypes.
7. `Services.I18N.Init(sceneTree)`.

`Start()` in the base only writes a log entry — the whole startup scenario is in the descendants.

### `ClientRootStarter`

Chosen when `--server` is **not** passed. This is the ordinary game process: the main menu, a
single-player game, connecting to someone else's server, and hosting "from inside the client".

`Init()` — after `base.Init()`:

| Step | What for |
|---|---|
| `ClientArgs.GetFromCmd(...)` | Parsing the client flags |
| `Services.Net.Init(false)` | The process is **not** a dedicated server → `Net.IsClient()` is always `true` |
| `Services.AutoScaling.Init(...)` | UI auto-scaling per `Consts.AutoScalingSettings` |
| `Services.LastGame.Init()` | Reading `resume-game.json` for the "Continue" button |
| `Services.GameSettings.Init()` | Reading `game-settings.json` |
| `--nick` / `--uid` | A temporary override of the nickname and the UID, **without writing** to the settings file |
| `Services.I18N.SetCurrentLocale(...)` | The locale from `GameSettings` |
| `Services.LoadingScreen.SetLoadingScreen(Loading)` | The first showing of the loading screen |

`Start()` — the scenario is chosen by the flags, exactly one of three:

| Condition | Action |
|---|---|
| `--auto-start` | `MainScene.StartSingleplayerGame(...)`. The save name comes from `--auto-start-savefile`, and if the flag is absent — a generated `SaveLoad.GenNewSaveFileName()` |
| `--auto-connect` | `MainScene.ConnectToMultiplayerGame(--auto-connect-ip, --auto-connect-port)` |
| otherwise | `MainScene.StartMainMenu()` + `LoadingScreen.Clear()` |

### `DedicatedServerRootStarter`

Chosen by the `--server` flag. There is no head player in this process.

`Init()` — after `base.Init()`:

| Step | What for |
|---|---|
| `DedicatedServerArgs.GetFromCmd(...)` | Parsing the server flags |
| `Services.Net.Init(true)` | The process is a dedicated server → `Net.IsClient()` is always `false` |
| `Services.LastGame.Init()` | Reading `resume-game.json` |
| `Services.DedicatedServerSettings.Init()` | Reading `dedicated-server-settings.json` |
| `Services.I18N.SetCurrentLocale(...)` | The locale from `DedicatedServerSettings`, **after** they are loaded |
| The window title | The `[SERVER]` prefix, so that the windows are not confused during a local test |

The differences from the client that are easy to forget: the dedicated server does **not** initialize
`Services.GameSettings` and `Services.AutoScaling` and does **not** show the loading screen — it has
nothing to show a player.

`Start()` — a single scenario: `MainScene.HostMultiplayerGameAsDedicatedServer(...)` with the save
name from `--savefile` (or a generated one), the port, the admin UID, `--parent-pid`, `--no-hud` and
`--world-render`.

## Level 2: GameStarter

`MainSceneService` creates the `Game` scene, puts it into `MainSceneContainer` and hands it a **game
starter** — an object that knows exactly how to bring this session up:

`Game.Init(BaseGameStarter starter)` calls `starter.Init(game)`, which sequentially performs:

* `game.AddNetwork()` — creates `Network` (ENet + `SceneMultiplayer`).
* `game.AddWorld()` — creates `World`.
* `game.AddHud()` / `game.AddServerHud()`.
* `ServerStartWorld()` — on the server: `StartNewGame()` or `LoadGame()`.
* `ClientStartWorld()` — on the client: `StartSyncWithServer()`.

The entry points in `Services.MainScene`, the starters behind them, and when each of them is used:

| Starter | `MainSceneService` method | Network / host | When it is used |
|---|---|---|---|
| `SingleplayerGameStarter` | `StartSingleplayerGame(saveFileName)` | none, `Network` is not created; the process is its own server | A single-player game from the menu, `--auto-start` (+ `--auto-start-savefile`) |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: false)` | an ENet server in this same process | Hosting "from inside the client" |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsDedicatedServer(...)` | an ENet server in this same process | A dedicated server (`--server`) |
| `ConnectToMultiplayerGameStarter` | `ConnectToMultiplayerGame(host, port)` | an ENet client, the host is a remote process | Connecting to a server from the menu, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: true)` | an ENet client + a child server process | Hosting with an out-of-process server: brings up a second OS process |

Which of these modes returns `true`/`false` from `Net.IsServer()` / `Net.IsClient()` and when — in
[Networking](Networking.md#process-roles-isserver--isclient).

### The common part: `BaseGameStarter`

Four protected methods that all the starters use:

* **`ServerStartWorld(world, saveFileName, adminUid)`** — the server-side world start. `saveFileName`
  is mandatory (`null` → `ArgumentNullException`). If the save file does not exist —
  `StartNewGame(...)`, if it does — `LoadGame(...)`. A `LoadException` during loading does not bring
  the process down: on the client it takes you back to the menu with the error text.
* **`ClientStartWorld(world)`** — the client-side start:
  `ClientStartStopService.StartSyncWithServer(...)`, that is, the handshake from
  [Networking](Networking.md).
* **`SetLastGame(...)` / `AddLastGameUpdaterToSaveEvent(...)`** — writing the session into
  `resume-game.json`. The second method subscribes to `SaveSuccessServerEvent` and updates the record
  with the name of the new save, so that "Continue" after a manual save leads to the current file.
* **`GoToMenuAndShowError(message)` / `GoToMenu()`** — returning to the menu. Both start with a
  `Net.IsClient()` check: there is no menu on a dedicated server.

### 1. `SingleplayerGameStarter`

A single-player game. **The network is not created at all** — `AddNetwork()` is not called, there is
no `Network` in the tree. The process is its own authority, `Net.IsServer()` returns `true`.

1. The `Loading` loading screen.
2. `AddWorld()`, `AddHud()`.
3. `resume-game.json` — the "single-player game" mode + a subscription to a successful save.
4. `ServerStartWorld(...)`, where the admin UID is `GameSettings.PlayerUid`: the player is an admin in
   their own game.
5. `ClientStartWorld(...)` — the same handshake as in a networked game, just local.

### 2. `HostMultiplayerGameStarter`

Brings up an ENet server **in this same process**. It is used in two completely different cases:
hosting "from inside the client" (`HostMultiplayerGameAsClient`) and a dedicated server
(`HostMultiplayerGameAsDedicatedServer`). The differences are set by constructor flags rather than by
a separate class.

Constructor parameters: `saveFileName`, `port`, `adminUid`, `parentPid`, `serverHudRender`,
`worldRender`, `mustSetLastGame`, `startedAsDedicated`.

1. The `Loading` loading screen.
2. If `parentPid` is passed — a `ProcessDeadChecker` (a node from KludgeBox) is attached to `Game`; it
   watches the parent process and calls `MainScene.Shutdown()` when the parent dies. This is how a
   child server avoids being left hanging after the client is closed (see [Shutdown](Shutdown.md)).
3. `AddNetwork()`, `AddWorld()`, `Net.DoClient(() => AddHud())` — the HUD only where there is a player.
4. `serverHudRender` → `AddServerHud()`; `worldRender == false` → `world.SetVisible(false)`. These are
   exactly the `--no-hud` and `--world-render` flags: by default a dedicated server draws the console,
   not the world.
5. `mustSetLastGame` → a write into `resume-game.json`. A server started from the console has none —
   there is nobody to write a "continue" for.
6. `network.HostServer(port ?? 25566, true)`. An error (for example, a busy port) → on the client, a
   return to the menu with the text; the starter goes no further.
7. `ServerStartWorld(...)` → `network.OpenServer()` → `Net.DoClient(() => ClientStartWorld(...))`.

> [!IMPORTANT]
> The order of step 7 is mandatory: the server is opened for incoming connections **only after** the
> world is up. Otherwise a client will manage to knock on a world that does not exist yet.

### 3. `ConnectToMultiplayerGameStarter`

Connecting to someone else's server. The only mode in which `Net.IsServer()` returns `false`.

Parameters: `host`, `port`, `mustSetLastGame`.

1. The `Connecting` loading screen — with a cancel button that calls `GoToMenu()`.
2. `AddNetwork()`, `AddWorld()`, `AddHud()`. The world is created right away, but **empty**: the
   snapshot from the server will fill it.
3. Subscriptions to `MultiplayerApi` events:
   * `ConnectedToServer` → `ClientStartWorld(...)`;
   * `ConnectionFailed` → to the menu with "Connection to the server failed" (the server did not
     answer within the timeout);
   * `ServerDisconnected` → to the menu with "Server disconnected" (this can arrive even hours into
     the game).
4. `mustSetLastGame` → a "connection to a server" write into `resume-game.json`.
5. `network.ConnectToServer(host ?? 127.0.0.1, port ?? 25566)`. A synchronous error is handled by the
   very same `ConnectionFailedEvent`.

> [!IMPORTANT]
> `ClientStartWorld` is called **on an event**, not immediately: at the time of `Init()` there is no
> connection yet. The `ConnectedToServer` handler is written as a local function and unsubscribes from
> the event on the very first firing — otherwise `SynchronizerService` leaks on the return to the menu
> (see [Code style conventions](../Code-style.md)).

### 4. `HostDedicatedServerAndConnectGameStarter`

Hosting with an out-of-process server: brings up a **second OS process** and connects to it as an
ordinary client. A descendant of `ConnectToMultiplayerGameStarter(Localhost, port,
mustSetLastGame: false)`.

1. `Services.Process.StartNewDedicatedServerApplication(...)` launches a new process with `--server`,
   passing it `--port`, `--savefile`, `--admin` and **`--parent-pid` with the PID of the current
   process**. `--headless` is set when the server window is not requested; log mirroring into the
   Godot console is never passed to the dedicated server.
2. A `ProcessShutdowner` (a node from KludgeBox) with the server's PID is attached to `Game`: when
   `Game` is destroyed, the child process will be killed.
3. `base.Init(game)` — from here on this is an ordinary connection to `127.0.0.1`.
4. The write into `resume-game.json` is done manually, **after** `base.Init`, as "own server". This is
   exactly why `mustSetLastGame: false` was passed to the base constructor: otherwise the base would
   have recorded the "connecting to someone else's server" mode, and the "Continue" button would stop
   bringing up a server.

The survivability of the process pair rests on two nodes at once: `ProcessShutdowner` kills the server
on a normal client shutdown, and `ProcessDeadChecker` on the server side is the safety net for the
case where the client died abnormally and had no time to kill anyone.
