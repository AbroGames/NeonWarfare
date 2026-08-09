# Startup flow

[← Project README](../README.md)

Two independent levels, not to be confused:

1. **RootStarter** — the **process** level, one per application lifetime: are we a client or a
   dedicated server.
2. **GameStarter** — the **game session** level, new on every entry into the game: host an ENet server,
   connect as an ENet client, or no network at all.

Command-line arguments are parsed **only** in the RootStarters and travel on as ordinary parameters
(see [Command-line arguments](Cli-args.md)).

## Level 1: RootStarter

`Root._Ready()` calls `RootStarterManager.ChooseStarter()`: `"--server"` in `OS.GetCmdlineArgs()` →
`DedicatedServerRootStarter`, otherwise `ClientRootStarter`. Both then get `Init()` (the common
`BaseRootStarter.Init()`, then the role-specific part: `Net.Init()`, the settings, the locale, UI
auto-scaling) and `Start()` (the scenario, through `Services.MainScene.*`). `RootData` (the containers,
`RootPackedScenes`, `SceneTree`) comes in as a parameter — no global access to `Root`.

### The common part: `BaseRootStarter`

`Init()` is the same for both roles and goes strictly in this order:

1. `Di.Process(this)`.
2. `CommonArgs`, log mirroring into the Godot console per `--godot-log-push`.
3. `Services.ExceptionHandler` — the global handler for unhandled exceptions.
4. Logging the command-line arguments that were received.
5. `Services.AssemblyCache` + `Services.TypesMapping`: without them the assembly-wide scans (the
   automatic pickup of `ICommandProcessor`, say) do not work.
6. `Services.LoadingScreen.Init(...)`, `Services.MainScene.Init(...)` — the services get the `Root`
   containers and the scene prototypes.
7. `Services.I18N.Init(sceneTree)`.

`Start()` in the base only logs — the scenario itself is in the descendants.

### `ClientRootStarter`

Chosen without `--server`: the ordinary game process — main menu, single-player game, connecting to
someone else's server, hosting "from inside the client". `Init()` after `base.Init()`:

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

`Start()` — exactly one of three, by the flags:

| Condition | Action |
|---|---|
| `--auto-start` | `MainScene.StartSingleplayerGame(...)`. The save name comes from `--auto-start-savefile`, and without that flag — a generated `SaveLoad.GenNewSaveFileName()` |
| `--auto-connect` | `MainScene.ConnectToMultiplayerGame(--auto-connect-ip, --auto-connect-port)` |
| otherwise | `MainScene.StartMainMenu()` + `LoadingScreen.Clear()` |

### `DedicatedServerRootStarter`

Chosen by `--server`. There is no head player in this process. `Init()` after `base.Init()`:

| Step | What for |
|---|---|
| `DedicatedServerArgs.GetFromCmd(...)` | Parsing the server flags |
| `Services.Net.Init(true)` | The process is a dedicated server → `Net.IsClient()` is always `false` |
| `Services.LastGame.Init()` | Reading `resume-game.json` |
| `Services.DedicatedServerSettings.Init()` | Reading `dedicated-server-settings.json` |
| `Services.I18N.SetCurrentLocale(...)` | The locale from `DedicatedServerSettings`, **after** they are loaded |
| The window title | The `[SERVER]` prefix, so that the windows are not confused during a local test |

Easy to forget: the dedicated server initializes neither `Services.GameSettings` nor
`Services.AutoScaling` and shows no loading screen — it has nothing to show a player.

`Start()` — a single scenario: `MainScene.HostMultiplayerGameAsDedicatedServer(...)` with the save name
from `--savefile` (or a generated one), the port, the admin UID, `--parent-pid`, `--no-hud` and
`--world-render`.

## Level 2: GameStarter

`MainSceneService` creates the `Game` scene, puts it into `MainSceneContainer` and hands it a **game
starter** — an object that knows how to bring this session up. `Game.Init(BaseGameStarter starter)`
calls `starter.Init(game)`, which performs in order: `game.AddNetwork()` (creates `Network` — ENet +
`SceneMultiplayer`), `game.AddWorld()`, `game.AddHud()` / `game.AddServerHud()`, then
`ServerStartWorld()` on the server (`StartNewGame()` or `LoadGame()`) and `ClientStartWorld()` on the
client (`StartSyncWithServer()`).

| Starter | `MainSceneService` method | Network / host | When it is used |
|---|---|---|---|
| `SingleplayerGameStarter` | `StartSingleplayerGame(saveFileName)` | none, `Network` is not created; the process is its own server | A single-player game from the menu, `--auto-start` (+ `--auto-start-savefile`) |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: false)` | an ENet server in this same process | Hosting "from inside the client" |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsDedicatedServer(...)` | an ENet server in this same process | A dedicated server (`--server`) |
| `ConnectToMultiplayerGameStarter` | `ConnectToMultiplayerGame(host, port)` | an ENet client, the host is a remote process | Connecting to a server from the menu, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: true)` | an ENet client + a child server process | Hosting with an out-of-process server: brings up a second OS process |

Which mode returns what from `Net.IsServer()` / `Net.IsClient()` — in
[Networking](Networking.md#process-roles-isserver--isclient).

### The common part: `BaseGameStarter`

Four protected methods that all the starters use:

* **`ServerStartWorld(world, saveFileName, adminUid)`** — the server-side world start. `saveFileName` is
  mandatory (`null` → `ArgumentNullException`); no such file → `StartNewGame(...)`, otherwise
  `LoadGame(...)`. A `LoadException` does not bring the process down: on the client it takes you back to
  the menu with the error text.
* **`ClientStartWorld(world)`** — the client-side start,
  `ClientStartStopService.StartSyncWithServer(...)`: the handshake from [Networking](Networking.md).
* **`SetLastGame(...)` / `AddLastGameUpdaterToSaveEvent(...)`** — writing the session into
  `resume-game.json`. The second subscribes to `SaveSuccessServerEvent` and updates the record with the
  new save name, so "Continue" after a manual save leads to the current file.
* **`GoToMenuAndShowError(message)` / `GoToMenu()`** — returning to the menu; both start with a
  `Net.IsClient()` check, since a dedicated server has no menu.

### 1. `SingleplayerGameStarter`

**The network is not created at all** — no `AddNetwork()`, no `Network` in the tree; the process is its
own authority and `Net.IsServer()` returns `true`.

1. The `Loading` loading screen.
2. `AddWorld()`, `AddHud()`.
3. `resume-game.json` — the "single-player game" mode + a subscription to a successful save.
4. `ServerStartWorld(...)` with `GameSettings.PlayerUid` as the admin UID: the player is an admin in
   their own game.
5. `ClientStartWorld(...)` — the same handshake as in a networked game, just local.

### 2. `HostMultiplayerGameStarter`

An ENet server **in this same process**, for two completely different cases: hosting "from inside the
client" (`HostMultiplayerGameAsClient`) and a dedicated server (`HostMultiplayerGameAsDedicatedServer`).
The difference is in the constructor flags rather than in a separate class: `saveFileName`, `port`,
`adminUid`, `parentPid`, `serverHudRender`, `worldRender`, `mustSetLastGame`, `startedAsDedicated`.

1. The `Loading` loading screen.
2. With `parentPid` — a `ProcessDeadChecker` (a KludgeBox node) on `Game`: it watches the parent process
   and calls `MainScene.Shutdown()` when it dies, so a child server is not left hanging after the client
   is closed (see [Shutdown](Shutdown.md)).
3. `AddNetwork()`, `AddWorld()`, `Net.DoClient(() => AddHud())` — the HUD only where there is a player.
4. `serverHudRender` → `AddServerHud()`; `worldRender == false` → `world.SetVisible(false)`. These are
   exactly `--no-hud` and `--world-render`: by default a dedicated server draws the console, not the
   world.
5. `mustSetLastGame` → a write into `resume-game.json`. A server started from the console has none —
   nobody to write a "continue" for.
6. `network.HostServer(port ?? 25566, true)`. An error (a busy port, say) → on the client, a return to
   the menu with the text; the starter goes no further.
7. `ServerStartWorld(...)` → `network.OpenServer()` → `Net.DoClient(() => ClientStartWorld(...))`.

> [!IMPORTANT]
> The order of step 7 is mandatory: the server is opened for incoming connections **only after** the
> world is up. Otherwise a client will manage to knock on a world that does not exist yet.

### 3. `ConnectToMultiplayerGameStarter`

Connecting to someone else's server — the only mode where `Net.IsServer()` returns `false`. Parameters:
`host`, `port`, `mustSetLastGame`.

1. The `Connecting` loading screen — with a cancel button that calls `GoToMenu()`.
2. `AddNetwork()`, `AddWorld()`, `AddHud()`. The world is created right away, but **empty**: the
   snapshot from the server will fill it.
3. Subscriptions to `MultiplayerApi` events: `ConnectedToServer` → `ClientStartWorld(...)`;
   `ConnectionFailed` → to the menu with "Connection to the server failed" (no answer within the
   timeout); `ServerDisconnected` → to the menu with "Server disconnected" (can arrive even hours into
   the game).
4. `mustSetLastGame` → a "connection to a server" write into `resume-game.json`.
5. `network.ConnectToServer(host ?? 127.0.0.1, port ?? 25566)`. A synchronous error is handled by that
   same `ConnectionFailedEvent`.

> [!IMPORTANT]
> `ClientStartWorld` is called **on an event**, not immediately: at `Init()` time there is no connection
> yet. The `ConnectedToServer` handler is a local function and unsubscribes on the very first firing —
> otherwise `SynchronizerService` leaks on the return to the menu (see
> [Code style conventions](Code-style.md)).

### 4. `HostDedicatedServerAndConnectGameStarter`

A **second OS process** plus an ordinary client connection to it. A descendant of
`ConnectToMultiplayerGameStarter(Localhost, port, mustSetLastGame: false)`.

1. `Services.Process.StartNewDedicatedServerApplication(...)` launches a process with `--server`,
   `--port`, `--savefile`, `--admin` and **`--parent-pid` with the PID of the current process**.
   `--headless` is set when the server window is not requested; log mirroring into the Godot console is
   never passed to the dedicated server.
2. A `ProcessShutdowner` (a KludgeBox node) with the server's PID is attached to `Game`: the child
   process is killed when `Game` is destroyed.
3. `base.Init(game)` — from here on this is an ordinary connection to `127.0.0.1`.
4. The write into `resume-game.json` is done manually **after** `base.Init`, as "own server". That is
   exactly why `mustSetLastGame: false` went to the base constructor: otherwise the base would have
   recorded "connecting to someone else's server" and "Continue" would stop bringing up a server.

Both nodes that kill this process pair, and from which side each works, are in [Shutdown](Shutdown.md).
