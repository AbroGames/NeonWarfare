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
`BaseRootStarter.Init()`, then the role-specific part: `SaveLoad.Init()`, the settings, the locale, UI
auto-scaling) and `Start()` (the scenario, through `Services.MainScene.*`). `RootData` (the containers,
`RootPackedScenes`, `SceneTree`) comes in as a parameter — no global access to `Root`.

### The common part: `BaseRootStarter`

`Init()` is the same for both roles and goes strictly in this order:

1. `Di.Process(this)`.
2. `CommonArgs`, log mirroring into the Godot console per `--godot-log-push`.
3. `Services.ExceptionHandler` — the global handler for unhandled exceptions.
4. Logging the command-line arguments that were received.
5. `Services.AssemblyCache` + `Services.TypesMapping`: the type mapping the network protocol and the entity kinds
   are built from (`NetMessageCodec`, `EntityCatalog`).
6. `Services.LoadingScreen.Init(...)`, `Services.MainScene.Init(...)` — the services get the `Root`
   containers and the scene prototypes.
7. `Services.QuitRequests.Init(sceneTree)` — only after `MainScene`, which the handlers call
   (see [Shutdown](Shutdown.md)).
8. `Services.I18N.Init(sceneTree)`.

`Start()` in the base only logs — the scenario itself is in the descendants.

### `ClientRootStarter`

Chosen without `--server`: the ordinary game process — main menu, single-player game, connecting to
someone else's server, hosting "from inside the client". `Init()` after `base.Init()`:

| Step | What for |
|---|---|
| `ClientArgs.GetFromCmd(...)` | Parsing the client flags |
| `Services.SaveLoad.Init(false)` | Restoring the save folder; autosave follows `GameSettings` |
| `Services.AutoScaling.Init(...)` | UI auto-scaling per `Consts.AutoScalingSettings` |
| `Services.LastGame.Init()` | Reading `resume-game.json` for the "Continue" button |
| `Services.KnownServers.Init()` | Reading `known-servers.json` for the server list of the multiplayer menu |
| `Services.GameSettings.Init()` | Reading `game-settings.json` |
| `--nick` / `--uid` | A temporary override of the nickname and the UID, **without writing** to the settings file |
| `Services.I18N.SetCurrentLocale(...)` | The locale from `GameSettings` |
| `Services.LoadingScreen.SetLoadingScreen(Loading)` | The first showing of the loading screen |

`Start()` — exactly one of three, by the flags:

| Condition | Action |
|---|---|
| `--auto-start` | `MainScene.StartSingleplayerGame(...)`. The save name comes from `--auto-start-savefile`, and without that flag — a generated `SaveLoad.GenNewSaveFileName()` |
| `--auto-connect` | `MainScene.ConnectToMultiplayerGame(--auto-connect-ip, --auto-connect-port)` |
| otherwise | `MainScene.StartMainMenu()`, which clears the loading screen once the menu is shown |

### `DedicatedServerRootStarter`

Chosen by `--server`. There is no head player in this process. `Init()` after `base.Init()`:

| Step | What for |
|---|---|
| `DedicatedServerArgs.GetFromCmd(...)` | Parsing the server flags |
| `Services.SaveLoad.Init(true)` | Restoring the save folder; autosave follows `DedicatedServerSettings` |
| `Services.LastGame.Init()` | Reading `resume-game.json` |
| `Services.DedicatedServerSettings.Init()` | Reading `dedicated-server-settings.json` |
| `Services.I18N.SetCurrentLocale(...)` | The locale from `DedicatedServerSettings`, **after** they are loaded |
| The window title | The `[SERVER]` prefix, so that the windows are not confused during a local test |

Easy to forget: the dedicated server initializes neither `Services.GameSettings` nor
`Services.AutoScaling` and shows no loading screen — it has nothing to show a player.

`Start()` — a single scenario: `MainScene.HostMultiplayerGameAsDedicatedServer(...)` with the save name
from `--savefile` (or a generated one), the port, the admin UID, `--parent-pid`, and `ServerHud` unless
the engine runs without a window (`DisplayServer.GetName()` is `headless`).

## Level 2: GameStarter

`MainSceneService` creates the `Game` scene, puts it into `MainSceneContainer` and hands it a **game starter** — an
object that knows how to bring this session up. `Game.Init(starter)` first creates what the session needs before
any World exists — `EntityCatalog` (from `WorldPackedScenes` and the type mapping), `NetMessageCodec` with the
protocol hash, the RepliCAT `Replicator` — then calls `starter.Init(game)`. The starters build the session from
`Game`'s methods:

* **`AddNetwork()`** — creates `Network` (ENet); single player has none.
* **`AddWorld(layers, origin, screen, saveFiles, localPlayer)`** — builds the World from the layer set and the
  origin (see [World](World.md)), puts it into `WorldContainer` and creates its screen: `Hud`, `ServerHud` or none.
  A World that throws while being built is freed.
* **`SendJoinRequest(localPlayer)`** — the join of this process's player. On the host its own peer first
  "connects", so it passes the same gatekeeper as a remote one (see [Networking](Networking.md#join-and-leave)).
* **`WorldSnapshotReceivedEvent`** — a remote client without a World got the join snapshot.

| Starter | `MainSceneService` method | Network | World | When it is used |
|---|---|---|---|---|
| `SingleplayerGameStarter` | `StartSingleplayerGame(saveFileName)` | none | `Host`, `Hud` | A single-player game from the menu, `--auto-start` (+ `--auto-start-savefile`) |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: false)` | an ENet server | `Host`, `Hud` | Hosting "from inside the client" |
| `DedicatedServerGameStarter` | `HostMultiplayerGameAsDedicatedServer(...)` | an ENet server | `Dedicated`, `ServerHud` or none | A dedicated server (`--server`) |
| `ConnectToMultiplayerGameStarter` | `ConnectToMultiplayerGame(host, port)` | an ENet client | `Client`, `Hud`, from the snapshot | Connecting to a server from the menu, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: true)` | an ENet client + a child server process | as above | Hosting with an out-of-process server |

### The common part: `BaseGameStarter`

* **`AddServerWorld(saveFileName, addWorld, out errorMessage)`** — the save file exists → `WorldOrigin.FromSave`,
  otherwise `WorldOrigin.NewWorld`; either way the World saves back to that file. A `LoadException` or a
  `SaveFormatException` is logged and returns `null` with a message for the player (another version of the game, or
  a broken save).
* **`ReadLocalPlayer()`** — reads the settings once into a `LocalPlayer`: the same object goes to the World and into
  the join request, so they cannot disagree.
* **`SetLastGame(...)` / `SaveFilesUpdatingLastGame(...)`** — writing the session into `resume-game.json`. The
  second wraps `Services.SaveLoad` into `LastGameUpdatingSaveFiles`, which points "Continue" at every file the World
  writes, so after a "save as" it leads to the new file.
* **`GoToMenuAndShowError(message)` / `GoToMenu()`** — returning to the menu; never called by
  `DedicatedServerGameStarter`, since a dedicated server has no menu.
* **`GoToMenuOnJoinRejected(game)`** — every starter with a player of its own: `Game.JoinRejectedEvent` → to the
  menu with the localized reason.

The admin of a server World (`WorldAdmin`) is the uid of the host's `LocalPlayer`, or `--admin` of a dedicated
server.

### 1. `SingleplayerGameStarter`

A host without `Network`: its only peer is its own.

1. The `Loading` loading screen.
2. `resume-game.json` — the "single-player game" mode, with the save files that keep it current.
3. `AddServerWorld(...)` → `AddWorld(Host, origin, Hud, ...)`. A load error → back to the menu with the message.
4. `GoToMenuOnJoinRejected(game)`, `SendJoinRequest(localPlayer)`, the loading screen is cleared.

### 2. `HostMultiplayerGameStarter` and `DedicatedServerGameStarter`

An ENet server **in this same process**. The common steps live in `BaseHostGameStarter`; the descendants differ in
the World, the screen and what happens on a failure and after `OpenServer()`.

1. The `Loading` loading screen.
2. Only the dedicated server, with `parentPid`: a `ProcessDeadChecker` (a GodotBox node) on `Game` calls
   `MainScene.Shutdown()` when the parent process dies, and so does the admin leaving (the starter is the World's
   `IDedicatedServerOwner`), so a child server is not left hanging after the client is closed (see
   [Shutdown](Shutdown.md)).
3. `AddNetwork()`; `mustSetLastGame` → a write into `resume-game.json` (a server started from the console has
   none).
4. `network.HostServer(port ?? 25566)` — the port is open but refuses connections. An error (a busy port, say) →
   the host goes back to the menu, the dedicated server shuts down.
5. `AddServerWorld(...)`: the host — `Host` with `Hud` and its `LocalPlayer`; the dedicated server — `Dedicated`
   with `ServerHud` or no screen, no local player, the World hidden. A load error → the host goes back to the menu,
   the dedicated server shuts down: a new world in its place would overwrite the save on exit.
6. `network.OpenServer()`.
7. Only the host: `SendJoinRequest(localPlayer)` (`GoToMenuOnJoinRejected` is subscribed before step 1), the
   loading screen is cleared.

> [!IMPORTANT]
> The server is opened for incoming connections **only after** the World is built. Otherwise a client would knock
> on a World that does not exist yet.

### 3. `ConnectToMultiplayerGameStarter`

Connecting to someone else's server. Parameters: `host`, `port`, `mustSetLastGame`.

1. The `Connecting` loading screen — with a cancel button that calls `GoToMenu()`.
2. `AddNetwork()`, `ReadLocalPlayer()`. **No World yet**: it is created from the server's join snapshot.
3. Subscriptions to the events of `Network` and `Game`, which die with the `Game`, so nothing unsubscribes:
   * `ConnectedToServerEvent` → `SendJoinRequest(localPlayer)`;
   * `WorldSnapshotReceivedEvent` → `AddWorld(Client, FromSnapshot(snapshot), Hud, null, localPlayer)` and the
     loading screen is cleared; a broken snapshot or a failure to build the World → back to the menu with the error;
   * `ConnectionFailedEvent` → to the menu with "Connection to the server failed" (no answer within the timeout);
   * `ServerDisconnectedEvent` → to the menu with "Server disconnected" (can arrive even hours into the game);
   * `GoToMenuOnJoinRejected(game)`.

   Each does nothing once its `Game` is queued for deletion: the multiplayer is still polled until the end of that
   frame.
4. `mustSetLastGame` → a "connection to a server" write into `resume-game.json`.
5. `network.ConnectToServer(host ?? 127.0.0.1, port ?? 25566)`. A synchronous error is handled by that same
   `ConnectionFailedEvent`.

### 4. `HostDedicatedServerAndConnectGameStarter`

A **second OS process** plus an ordinary client connection to it. A descendant of
`ConnectToMultiplayerGameStarter(Localhost, port, mustSetLastGame: false)`.

1. `Services.Process.StartNewDedicatedServerApplication(...)` launches a process with `--server`, `--port`,
   `--savefile`, `--admin` (the uid of the `LocalPlayer` this starter joins with) and **`--parent-pid` with the PID of
   the current process**. `--headless` is set when the server window is not requested; log mirroring into the Godot
   console is never passed to the dedicated server. `ProcessService` keeps the PID to wait for the process to exit.
2. `base.Init(game)` — from here on this is an ordinary connection to `127.0.0.1`.
3. The write into `resume-game.json` is done manually **after** `base.Init`, as "own server". That is exactly why
   `mustSetLastGame: false` went to the base constructor: otherwise the base would have recorded "connecting to
   someone else's server" and "Continue" would stop bringing up a server.

How this process pair is stopped is in [Shutdown](Shutdown.md).
