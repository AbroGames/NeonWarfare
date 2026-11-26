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
object that knows how to bring this session up. `Game.Init(starter)` first creates `GameProtocol` — what the
session needs before any World exists: `EntityCatalog` (from `WorldPackedScenes` and the type mapping),
`NetMessageCodec` with the protocol hash, the RepliCAT `Replicator` — and the `WorldAssembler` over it, then calls
`starter.Start(game)`. Each starter is one top-to-bottom script over `Game`'s methods, one set per mode, so no step
can come too early.

### `Game`: the session API

* **`AddServerNetwork()`** — `Network` (ENet) of a host or a dedicated server; single player has none.
* **`ConnectToServer(localPlayer, host, port)`** — `Network` of a remote client and its `ClientTransport`, which
  sends the join once connected. **No World yet**: `Game` builds it from the join snapshot itself.
* **`AddHostWorld(saveFiles, localPlayer, origin)`** / **`AddDedicatedWorld(saveFiles, admin, origin)`** — the World
  of its configuration (`WorldSetup`, see [World](World.md)) over its transport, put into `WorldContainer`.
  `AddHostWorld` also sends the host's join: its own peer first "connects", so it passes the same gatekeeper as a
  remote one (see [Networking](Networking.md#join-and-leave)).
* **`ShowServerHud()`** — the screen of a dedicated server.

`WorldAssembler` builds each configuration: its transports, `WorldDependencies`, `InitPreReady`. A World that throws
while being built is freed and its `ServerTransport` detached from the network.

`Game` is both ports of its World, `ILocalPlayerOwner` and `IServerOwner`, and reports up through three events:

| Event | When |
|---|---|
| `LocalPlayerJoined` | The World reported its own `PlayerJoinedEvent`: `Game` has shown the `Hud`, so the UI never sees "me" offline |
| `Failed(message)` | The join is rejected, the `Hud` or the client World fails to be created, the snapshot is broken, the connection fails or the server disconnects |
| `AdminLeft` | The admin of the server World has left, inside the tick |

None is raised once the `Game` is queued for deletion: the multiplayer is still polled until the end of that frame.
The handlers die with the `Game`, so nothing unsubscribes.

| Starter | `MainSceneService` method | Network | World | When it is used |
|---|---|---|---|---|
| `SingleplayerGameStarter` | `StartSingleplayerGame(saveFileName)` | none | `Host`, `Hud` | A single-player game from the menu, `--auto-start` (+ `--auto-start-savefile`) |
| `HostMultiplayerGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: false)` | an ENet server | `Host`, `Hud` | Hosting "from inside the client" |
| `DedicatedServerGameStarter` | `HostMultiplayerGameAsDedicatedServer(...)` | an ENet server | `Dedicated`, `ServerHud` or none | A dedicated server (`--server`) |
| `ConnectToMultiplayerGameStarter` | `ConnectToMultiplayerGame(host, port)` | an ENet client | `RemoteClient`, `Hud`, from the snapshot | Connecting to a server from the menu, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | `HostMultiplayerGameAsClient(..., createDedicatedServerProcess: true)` | an ENet client + a child server process | as above | Hosting with an out-of-process server |

### The shared steps: `BaseGameStarter`

Only helpers, no template method:

* **`LoadServerOrigin(saveFileName)`** — the save file exists → `WorldOrigin.FromSave`, otherwise
  `WorldOrigin.NewWorld`; either way the World saves back to that file. Each server starter wraps it with
  `Add…World` in a `try` whose `catch` takes **`IsLoadError(e)`** (a `LoadException` or a `SaveFormatException`);
  **`LogLoadError(e, saveFileName)`** logs it and gives the message for the player.
* **`ReadLocalPlayer()`** — reads the settings once into a `LocalPlayer`: the same object goes to the World and into
  the join request, so they cannot disagree.
* **`SetLastGame(...)` / `SaveFilesUpdatingLastGame(...)`** — writing the session into `resume-game.json`. The
  second wraps `Services.SaveLoad` into `LastGameUpdatingSaveFiles`, which points "Continue" at every file the World
  writes, so after a "save as" it leads to the new file.
* **`FollowLocalPlayer(game)`** — `Failed` → the menu with the message, `LocalPlayerJoined` → the loading screen is
  cleared.
* **`ConnectAndFollow(game, localPlayer, host, port)`** — the `Connecting` loading screen with a cancel button that
  calls `GoToMenu()`, `FollowLocalPlayer`, `game.ConnectToServer(...)`.
* **`GoToMenuAndShowError(message)` / `GoToMenu()`** — never called by `DedicatedServerGameStarter`, since a
  dedicated server has no menu.

The admin of a server World (`WorldAdmin`) is the uid of the host's `LocalPlayer`, or `--admin` of a dedicated
server.

### 1. `SingleplayerGameStarter`

A host without `Network`: its only peer is its own.

1. The `Loading` loading screen.
2. `resume-game.json` — the "single-player game" mode, with the save files that keep it current.
3. `FollowLocalPlayer`, then `AddHostWorld(...)`, which sends the join; it is applied in the next tick. A load error
   → back to the menu with the message.

### 2. `HostMultiplayerGameStarter` and `DedicatedServerGameStarter`

An ENet server **in this same process**.

1. The `Loading` loading screen, `AddServerNetwork()`.
2. `resume-game.json` — "own server" with the save files that keep it current: always on the host, on the dedicated
   server only with `parentPid` (a server started from the console is never resumed). The dedicated server with
   `parentPid` is a child server, set up in one place, `FollowParentClient`: a `ProcessDeadChecker` (a GodotBox node)
   on `Game` and `AdminLeft` both call `MainScene.Shutdown()`, so a child server is not left hanging after the client
   is closed (see [Shutdown](Shutdown.md)).
3. `network.HostServer(port ?? 25566)` — the port is open but refuses connections. An error (a busy port, say) →
   the host goes back to the menu, the dedicated server shuts down.
4. The host: `FollowLocalPlayer`.
5. The host — `AddHostWorld(...)` with its `LocalPlayer`, whose join leaves before the server is opened; the dedicated
   server — `AddDedicatedWorld(...)`, no local player, the World hidden, then `ShowServerHud()` if requested. A load
   error → the host goes back to the menu, the dedicated server shuts down: a new world in its place would overwrite
   the save on exit.
6. `network.OpenServer()`.

> [!IMPORTANT]
> The server is opened for incoming connections **only after** the World is built. Otherwise a client would knock
> on a World that does not exist yet.

### 3. `ConnectToMultiplayerGameStarter`

Connecting to someone else's server. Parameters: `host`, `port`.

1. `resume-game.json` — "connection to a server".
2. `ConnectAndFollow(game, ReadLocalPlayer(), host ?? 127.0.0.1, port ?? 25566)`. The `Hud` comes with the events
   packet of the join tick, right after the snapshot. A failed connection (no answer within the timeout, or a
   synchronous error), a server disconnect (even hours into the game), a broken snapshot → back to the menu.

### 4. `HostDedicatedServerAndConnectGameStarter`

A **second OS process** plus an ordinary client connection to it.

1. `ReadLocalPlayer()`; `Services.Process.StartNewDedicatedServerApplication(...)` launches a process with
   `--server`, `--port`, `--savefile`, `--admin` (the uid of that `LocalPlayer`) and **`--parent-pid` with the PID of
   the current process**. `--headless` is set when the server window is not requested; log mirroring into the Godot
   console is never passed to the dedicated server. `ProcessService` keeps the PID to wait for the process to exit.
2. `resume-game.json` — "own server", so that "Continue" brings the server up again rather than connecting to it.
   The child server writes the same mode itself and keeps it current after a "save as"; `Services.LastGame` re-reads
   the file on every access, so "Continue" in this process follows it.
3. `ConnectAndFollow(game, localPlayer, 127.0.0.1, port)` — from here on this is an ordinary connection.

How this process pair is stopped is in [Shutdown](Shutdown.md).
