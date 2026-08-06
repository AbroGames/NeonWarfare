# Neon Warfare

A cooperative top-down action game built with Godot and C#.
Supported OSes: Windows and Linux.

---

## Table of Contents

- [Stack and dependencies](#stack-and-dependencies)
- [Quick start](#quick-start)
- [Command-line arguments](#command-line-arguments)
- [Architecture](#architecture)
  - [Layers and the scene tree](#layers-and-the-scene-tree)
  - [Startup flow](#startup-flow)
  - [Game session startup modes](#game-session-startup-modes)
  - [Networking](#networking)
  - [Data and saves](#data-and-saves)
  - [Entities](#entities)
  - [Services](#services)
  - [Dependency injection (DI)](#dependency-injection-di)
  - [User interface](#user-interface)
  - [Chat and commands](#chat-and-commands)
  - [Shutdown](#shutdown)
- [Repository structure](#repository-structure)
- [Coding conventions](#coding-conventions)
- [Localization](#localization)
- [Debugging and performance](#debugging-and-performance)
- [Current state](#current-state)

---

## Stack and dependencies

| Component | Version / purpose |
|---|---|
| Godot | Latest version, Forward Plus renderer |
| .NET | Latest version, assembly name `NeonWarfare` |
| KludgeBox | In-house library: DI, logging, networking utilities, Godot nodes, Godot extensions |
| CommunityToolkit.Mvvm | `[ObservableProperty]` source generator for world data |
| MessagePack | Binary serialization of world state for saves and network transfer |

Key engine settings (`project.godot`):
- physics: **30 ticks/sec**, gravity disabled, `physics_interpolation = true`
- locales: `en`, `ru` (`Assets/Locales/*.po`)
- main scene: `Scenes/Root/Root.tscn`

## Quick start

> [!NOTE]
> It is recommended to install and update Godot via [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases): it automatically configures all the ENV variables used by the project in `launchSettings.json`.
> To set up the Rider integration in Godot, go to Editor → Editor Settings → Dotnet → Editor.
> In the External Editor list, pick JetBrains Rider and clear the Custom Exec Path Args value.

1. Install the latest version of **Godot .NET** and the **.NET SDK**.
2. Open `project.godot` in Godot (NuGet packages are restored on the first build), or build via `NeonWarfare.sln`.
3. Run the project (`F5`) — a client with the main menu opens.

For quick multiplayer testing, set up the following run configurations in Rider:

* Type: `.Net Executable`. Name: `Server`. Program Arguments: `--server`.
* Type: `.Net Executable`. Name: `Autoconnect (1)`. Program Arguments: `--auto-connect --uid TestPlayer1 --nick TestPlayer1`.
* Type: `.Net Executable`. Name: `Autoconnect (2)`. Program Arguments: `--auto-connect --uid TestPlayer2 --nick TestPlayer2`.
* Type: `Multi-Launch`. Name: `Fast-test (1 client)`. Tasks: `Server, Autoconnect (1)`.
* Type: `Multi-Launch`. Name: `Fast-test (2 clients)`. Tasks: `Server, Autoconnect (1), Autoconnect (2)`.

> [!IMPORTANT]
> The second client must have its own `--uid`: the server rejects a connection if a player with the same UID is already online.

## Command-line arguments

Arguments are described in `Scripts/Content/CmdArgs/` and parsed **only** in the `RootStarter`s; afterwards they are passed around as plain parameters.

**Common** (`CommonArgs`):

| Flag | Description |
|---|---|
| `--godot-log-push` | Mirror Serilog logs to the Godot console |

**Client** (`ClientArgs`):

| Flag | Description |
|---|---|
| `--auto-start` | Immediately start a singleplayer game with a new save file, bypassing the menu |
| `--auto-connect` | Immediately connect to a server, bypassing the menu (address and port are set by other flags) |
| `--auto-connect-ip <ip>` | Server address for auto-connect (defaults to `127.0.0.1` if omitted) |
| `--auto-connect-port <port>` | Port for auto-connect (defaults to `25566` if omitted) |
| `--nick <nick>` | Temporarily (without persisting to settings) override the nickname |
| `--uid <uid>` | Temporarily (without persisting to settings) override the player UID |

**Dedicated server** (`DedicatedServerArgs`):

| Flag | Description |
|---|---|
| `--server` | Run the process as a dedicated server (selects `DedicatedServerRootStarter`) |
| `--headless` | Run without a window |
| `--port <port>` | Port the server listens on (defaults to `25566` if omitted) |
| `--savefile <name>` | Save file name; if the file does not exist, a new game is created |
| `--admin <uid>` | UID of the player who will receive administrator privileges |
| `--parent-pid <pid>` | PID of the parent process; the server shuts down when the parent dies |
| `--no-hud` | Do not render the `ServerHud` |
| `--world-render` | Render the game world (by default the world is hidden and only the server console is visible) |

---

## Architecture

### Layers and the scene tree

The project is built as a strict "container → content" hierarchy. Each level knows only about its descendants; content swapping is done through `NodeContainer` (a wrapper over `KludgeBox.Godot.Nodes.NodeContainer`).

```
Root (Node2D)                       — entry point, lives for the whole application session
├── MainSceneContainer              — contains MainMenu OR Game
│   └── MainMenu | Game
├── LoadingScreenContainer          — loading screen on top of everything (CanvasLayer)
└── PackedScenes (RootPackedScenes) — prototypes of Game / MainMenu / LoadingScreen

Game (Node2D)                       — one game session (singleplayer or networked)
├── WorldContainer → World          — the container holds World
├── HudContainer                    — contains Hud OR ServerHud
│   └── Hud | ServerHud
├── PackedScenes (GamePackedScenes) — prototypes of World / Hud / ServerHud
└── Network                         — created from code, lives alongside Game

World (Node2D, IServiceProvider)    — the game world and all its services
├── Tree (WorldTree)                — container of the current surface
│   └── Surface (SafeSurface | BattleSurface)
│       └── Character, Wall, ...    — game objects, synchronized by MultiplayerSpawner
├── PersistenceData                 — data that goes into a save
├── TemporaryData                   — data for the current session
├── *Service                        — world services (see below)
├── SyncedPackedScenes              — prototypes of scenes allowed for network spawning
└── ClientPackedScenes              — prototypes of purely client-side (visual) scenes
```

> [!IMPORTANT]
> **The client and the server share the same scene tree.**
> The role is determined at runtime via `Net.IsServer()` / `Net.IsClient()`
> and through the helpers `Net.DoClient(...)`, `Net.DoServerClient(...)`, `Net.DoServerNotServer(...)`.

Control-flow rule: **calls go down the tree, events (`event` / signals) go up.**
A parent knows about its children, a child does not know about its parent (the exception is an explicit `[Parent]` injection in world services).

### Startup flow

```
Root._Ready()
  └── RootStarterManager.ChooseStarter()      — is "--server" in OS.GetCmdlineArgs()?
        ├── ClientRootStarter                 — no  → client
        └── DedicatedServerRootStarter        — yes → dedicated server
              │
              ├── Init()  — shared BaseRootStarter.Init(): exception handler, assembly cache,
              │             type mapping, LoadingScreenService, MainSceneService, I18N;
              │             then the specific bits: Net.Init(), settings, locale, UI autoscaling
              └── Start() — launch the desired scenario via Services.MainScene.*
```

Then `MainSceneService` creates the `Game` scene and hands it a **game starter** — an object that knows how exactly to bring this session up:

```
Game.Init(BaseGameStarter starter) → starter.Init(game)
  ├── game.AddNetwork()  — creates Network (ENet + SceneMultiplayer)
  ├── game.AddWorld()    — creates World
  ├── game.AddHud() / game.AddServerHud()
  ├── ServerStartWorld() — on the server: StartNewGame() or LoadGame()
  └── ClientStartWorld() — on the client: StartSyncWithServer()
```

### Game session startup modes

| Starter | Network | Who hosts | When it is used |
|---|---|---|---|
| `SingleplayerGameStarter` | none (`Network` is not created) | the process is its own server | Singleplayer from the menu, `--auto-start` |
| `HostMultiplayerGameStarter` | ENet server | the same process | Hosting "from inside the client" and the dedicated server |
| `ConnectToMultiplayerGameStarter` | ENet client | a remote process | Connecting to a server, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | ENet client + child process | a separate server process | Hosting with an external server: spawns a second process with `--server` and connects to it |

In singleplayer and in the main menu `Net.IsServer()` returns `true` — the process is its own authority.
It returns `false` **only** when we are connected as a client to someone else's server.
This lets you write server logic once and not duplicate it for singleplayer.

### Networking

The project uses **Godot's built-in high-level multiplayer** (`SceneMultiplayer` + `ENetMultiplayerPeer`).
The custom packet bus (`[GamePacket]`, `SC_`/`CS_` classes, `[EventListener]`) has been removed entirely.

* **`Network`** ([Scenes/Game/Network/Network.cs](Scenes/Game/Network/Network.cs)) — a wrapper over `MultiplayerApi`.
  Creates a new `SceneMultiplayer` bound to the `Game` node, so that on exit to the menu all old subscriptions are guaranteed to drop.
  Handles `ConnectToServer()`, `HostServer()`, `OpenServer()`, and a clean `Shutdown()`.
* **`NetworkStateMachine`** — states `NotInitialized → Connecting/Hosting → Connected/Hosted → Disconnected`
  and derived flags (`IsClient`, `IsServer`, `IsActiveGameState`).
* **RPC** — the primary exchange mechanism. Pairs of "public wrapper + private `*Rpc`":

  ```csharp
  public void Save(string saveFileName) => RpcId(ServerId, MethodName.SaveRpc, saveFileName);

  [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
  private void SaveRpc(string saveFileName) { /* ... */ }
  ```

* **Object spawning** — `WorldMultiplayerSpawnerService.AddSpawnerToNode(node)` attaches a
  `WorldMultiplayerSpawner` (a subclass of KludgeBox's `AbstractMultiplayerSpawner`) to a node.
  The spawner is automatically removed together with the observed node.
  Only scenes listed in `SyncedPackedScenes` can be spawned.
* **Field synchronization** — the `[Sync]` attribute from `KludgeBox.Godot.Nodes.MpSync`
  (e.g. `WorldTemporaryData.PlayerUidByPeerId`).
* **Transfer channels** — `Consts.TransferChannel`: `Chat`, `StatsHp`, `StatsCache`. Specified in
  `[Rpc(TransferChannel = (int) Consts.TransferChannel.X)]` so that "chatty" streams do not interfere with each other.
* **Payload** — Godot primitives or a `byte[]` from MessagePack. The maximum sync-packet size is
  `Network.MaxSyncPacketSize`.

**Client connection** (`WorldSynchronizerService`) — the same for networked and singleplayer games:

```
client: StartSyncOnClient(uid, nick, color)
   → server: NewClientInitOnServerRpc — validation (uid uniqueness, nick length 3..25,
             no spaces, color lightness ≥ 0.2), register PlayerData, grant admin
             ├── reject  → RejectSyncOnClientRpc(error) → client returns to the menu with a message
             └── success → EndSyncOnClientRpc(byte[]) — the whole PersistenceData as one MessagePack snapshot
   → client: deserialize the world → EndSyncOnServerRpc
   → server: WorldPlayerService.SpawnPlayer(peerId)
```

### Data and saves

World data is split into two nodes and **contains no logic** — only state and its synchronization:

| Node | Lives | Synchronization | Goes into a save |
|---|---|---|---|
| `WorldPersistenceData` | until the end of the game | RPCs inside storage classes + a snapshot on connect | **Yes** |
| `WorldTemporaryData` | until the end of the session | `[Sync]` | No |

`WorldPersistenceData` consists of storage nodes (`GeneralDataStorage`, `PlayerDataStorage`), each of which
implements `ISerializableStorage` (`SerializeStorage` / `DeserializeStorage` / `SetAllPropertyListeners`).
`WorldDataSerializerService` uses reflection (`Services.MembersScanner`) to walk all `ISerializableStorage`
inside `WorldPersistenceData` and collects a `Dictionary<string, byte[]>` → MessagePack. That same byte blob
is used both for saving to disk and for the initial sync of a new client.

The models themselves (`PlayerData`, `GeneralData`) are `ObservableObject`s with `[ObservableProperty]` and
MessagePack `[Key(N)]` keys. The storage subscribes to `PropertyChanged` and automatically broadcasts the
change to clients — meaning **any write to a model property on the server is networked by itself**.

Saves: `user://saves/<name>.bin`, the new file name is `yyyy-MM-dd_HH-mm` (`SaveLoadService`).
Auto-saving is performed in `WorldServerShutdowner` on exit from the tree and is controlled by the
`AutoSaveEnabled` setting (on the client it is `GameSettings`, on the dedicated server — `DedicatedServerSettings`).

Other files in `user://` (JSON): `game-settings.json` — client settings,
`dedicated-server-settings.json` — dedicated server settings, `resume-game.json` — the last
session for the "Resume" button.

### Entities

A `Character` (`RigidBody2D`) is assembled from independent subsystems, each of which has a **server and a client version**:

| Subsystem | Server | Client |
|---|---|---|
| Stats | `CharacterStats` | `CharacterStatsClient` |
| Status effects | `CharacterStatusEffects` | `CharacterStatusEffectsClient` |
| Control | `CharacterController` (shared, the data source can be either side) | — |

The version is chosen in `Character._Ready()` via `Net.DoServerClient(...)`.
The bridge between the sides is `CharacterSynchronizer` — a node split into `partial` files by subsystem
(`CharacterSynchronizer_Stats.cs`, `_Controller.cs`, `_StatusEffects.cs`). **The synchronizer must contain
no logic other than networking.**

**Controllers** (`IController`):

* `PlayerController` — the local player: reads input, computes physics (`PhysicsCalculator`), and sends
  `MovementData` (position, rotation, velocity, a monotonic `OrderId`);
* `RemoteController` — the default for all foreign objects: extrapolates the last `MovementData`,
  ignores stale packets by `OrderId`, and teleports the object when the discrepancy exceeds `DistanceForTeleport`;
* `AiController` — a subclass of `PlayerController` that replaces the "input" source with an `IAiControllerLogic`
  (`AiBattleControllerLogic`, `AiMoveControllerLogic`, `AiObserveControllerLogic`, `AiPatrolControllerLogic`).

Control blockers — `ControlBlocker` (`MenuIsOpen`, `ChatIsOpen`, `CharacterIsDead`, `CharacterIsStunned`,
`CharacterIsRooted`, `CharacterIsSilenced`), each blocks a subset of movement / rotation / skills;
they are combined in `ControlBlockerHandler`. Teleporting should only be done via `CharacterController.Teleport()` —
it correctly resets physics and interpolation.

**Stats** — the `CharacterStat` enum (`MaxHp`, `RegenHp`, `Armor`, `MovementSpeed`, `SkillDamage`, …)
over KludgeBox's `StatModifiersContainer`: additive and multiplicative modifiers, while `CharacterStats`
exposes already-clamped values to the outside. `Hp`, `DutyHp`, death, and resurrection live there too.

**Status effects** — `StatusEffect` with a fluent builder (`Id`, `Tags`, `DisplayName`, `Description`, `IconName`,
`Type`, `IsVisual`, `Time`, `IsFinishCondition`) and an adding policy `IAddingStatusEffectPolicy`
(`LimitByIdAddingPolicy`, `LimitByTagAddingPolicy`, `NoCheckAddingPolicy`, `UpdateTimeAddingStatusEffectPolicy`).
Ready-made implementations: poison, heal, stat change, control block, resurrection.

### Services

**Global services** — the static class `Services` ([Scripts/Services.cs](Scripts/Services.cs)), accessible from
anywhere. Some come from KludgeBox (`Di`, `Rand`, `Math`, `NodeTree`, `I18N`, `AutoScaling`, `AssemblyCache`,
`TypesMapping`, `ExceptionHandler`, `StringCompression`), some are game-specific:

| Service | Purpose |
|---|---|
| `Services.Net` | Process role detection (`IsClient`/`IsServer`), helpers `DoClient`, `DoServerClient` |
| `Services.MainScene` | Switching MainMenu ↔ Game, entry points for all game modes, `Shutdown()` |
| `Services.LoadingScreen` | Show / hide the loading screen |
| `Services.GameSettings` | Client settings + temporary `--nick` / `--uid` |
| `Services.DedicatedServerSettings` | Dedicated server settings |
| `Services.MenuGameSettings` | Bridge between `GameSettings` and the settings-screen model (`MenuGameSettings`) |
| `Services.SaveLoad` | Working with save files, `SaveException` / `LoadException` |
| `Services.LastGame` | The last session for the "Resume" button (`ResumableGame`) |
| `Services.Process` | Launching the dedicated-server child process |
| `Services.IconsStorage` | Icon identifiers |

`Services.Di` and `Services.Net` are additionally exposed via `Services.Global` and wired up with a
`global using static` — so in code you simply write `Di.Process(this)` and `Net.IsServer()`.
The same place also exposes `Consts.Global` (`ServerId`, `BroadcastId`) and the Godot extensions from KludgeBox
(vectors, colors, camera, nodes — hence, for example, `Vec2(x, y)`) — see
[Scripts/GlobalUsings.cs](Scripts/GlobalUsings.cs). New global imports are added only there.

**World services** — child nodes of `World`. `World` itself implements `IServiceProvider` and in `_EnterTree()`
registers them in a type-keyed dictionary, which makes `[SceneService]` injection possible.

| Service | Purpose |
|---|---|
| `WorldServerStartStopService` | Server start: new game / load, initialize the synchronizer and commands |
| `WorldClientStartStopService` | Client start: sync with the server, loading screen, ping |
| `WorldSynchronizerService` | Client handshake, player validation, initial world transfer |
| `WorldMultiplayerSpawnerService` | Attaching `MultiplayerSpawner`s to nodes |
| `WorldDataSaveLoadService` | Save / load, save permissions, autosave |
| `WorldDataSerializerService` | (De)serialization of `WorldPersistenceData` |
| `WorldChatService` | Chat, 100-message history, interceptors |
| `WorldCommandService` | Chat commands, auto-discovery of all `ICommandProcessor`s in the assembly |
| `WorldPlayerService` / `WorldEnemyService` | Spawning players and bots (common base `WorldCharacterService`) |
| `WorldPerformanceService` | Godot / .NET / ENet / ping metrics |
| `WorldFacadeService` | Facade for frequent aggregate queries (player data, online/offline, `IsAdmin`) |

> [!NOTE]
> `World` is a service container. Each service may reference other services and is a point of
> interaction with the system: when you call its method, you should get a consistent state of the whole system,
> not just of that service.

### Dependency injection (DI)

The project uses KludgeBox's DI. Practically every class calls `Di.Process(this)` as the first line of `_Ready()`
(or in the constructor for non-nodes), after which the annotated fields are populated:

| Attribute | What it injects |
|---|---|
| `[Child]` | A child node by field name (or `[Child(By.Type)]` — by type) |
| `[Parent]` | The parent node of the required type |
| `[SceneService]` | A service from the nearest `IServiceProvider` up the tree (i.e. from `World`) |
| `[Logger]` | A `Serilog.ILogger` configured for the current class |
| `[NotNull]` | A check that an `[Export]` field is filled in the editor (in `CheckedAbstractStorage`) |

`CheckedAbstractStorage` is the base for all `PackedScene` storages (`RootPackedScenes`, `GamePackedScenes`,
`SyncedPackedScenes`, `ClientPackedScenes`, `PagesProvider`). References to scene prototypes are configured in
the Godot editor; obtaining any scene for instantiation starts here.

### User interface

The main menu is built on a **page stack** (`Scenes/Screen/NewMenu/`):

* `PageContainer` — holds the stack, supports `SetRootPage` / `PushPage` / `PopPage`, detects cycles;
* `Page` / `IPage` — a page with `Parent`/`Child` links and `OnShown` / `OnHidden` / `Close` callbacks;
* `MainMenuPage` — the base for menu pages, receives `PagesProvider` via `WithAvailablePages(...)`;
* `PagesProvider` — a storage of `PackedScene`s for all pages (Main, Settings, Connect, Host, Singleplayer,
  Message, LanguageSelection).

The settings screen is built **from a model, not by hand**: `MenuGameSettings` describes the fields, and the
`[Name]`, `[Hint]`, `[Hide]`, `[Range]`, `[Step]` attributes control display. `GetVisibleSettings()` collects
the list of `Setting`s via reflection, from which `SettingsPage` generates the controls.

The in-game HUD: `Hud` (client) and `ServerHud` (server console) — both receive `World` via `InitPreReady(world)`
before being added to the tree. The loading screen (`LoadingScreen`) lives in a separate `CanvasLayer` on top
of everything and supports an optional cancel button.

### Chat and commands

A message flows `client → server (TrySendNewMessageRpc) → interceptors → broadcast`.
An `IChatMessageInterceptor` can "swallow" a message: for example, `ChatMessageCommandInterceptor`
takes anything starting with `/` and hands it to `WorldCommandService`.

Commands are classes implementing `ICommandProcessor` (`GetCommand`, `GetDescription`, `IsRequiringAdmin`,
`ProcessCommand`). They are **registered automatically**: on startup the service scans the assembly and collects
all implementations, logging warnings on duplicates. To add a command, just create a class in
`Scenes/World/Service/Command/Impl/`.

Current set: `/help`, `/players`, `/uids`, `/admins`, `/admin {add|remove} <nickname>` (admin),
`/surface {safe|battle}` (admin).

### Shutdown

* **Client** — `Services.MainScene.Shutdown()` → a deferred `SceneTree.Quit()`.
* **Server child process** — when the `Game` scene is destroyed, `ProcessShutdowner` fires,
  which kills the server process by its saved PID.
* **Server with `--parent-pid`** — `ProcessDeadChecker` periodically checks whether the parent is alive and
  shuts the server down if the client that launched it has disappeared from the OS.
* **World save** — `WorldServerShutdowner` catches `NotificationExitTree` and calls `TryAutoSave()`.
  A separate node is needed because, after exit from the tree, `GetMultiplayer()` is already `null`, and `Network`
  may have swapped the peer for an `OfflineMultiplayerPeer` by that point — they cannot be trusted.

---

## Repository structure

```
Assets/            Resources: fonts, locales (.po), UI theme, shaders, textures
Scenes/            Scenes (.tscn) and their handlers (.cs) — kept together, in the same folder
├── Root/          Application entry point and process starters
├── Game/          Game session: network and game-mode starters
├── World/         The world: tree, surfaces, data, services
├── Entity/        Game objects: characters (controllers, stats, effects), walls
├── Screen/        UI: main menu, HUD, server console, loading screen
└── KludgeBox/     Thin subclasses of KludgeBox nodes for the project's needs
Scripts/           Code without scenes
├── Content/       Command-line argument definitions, loading-screen types
├── Service/       Global services, settings, saves
├── Services.cs    Registry of all global services
├── Consts.cs      Global constants and transfer channels
└── GlobalUsings.cs
```

**A scene and its handler live in the same folder** and share the name: `Hud.tscn` + `Hud.cs`.
Large handlers are split into `partial` classes by functionality with an `_` suffix:
`CharacterSynchronizer.cs`, `CharacterSynchronizer_Stats.cs`, `CharacterSynchronizer_Controller.cs`.

Namespaces mirror the file path: `Scenes/World/Service/Chat/WorldChatService.cs` →
`NeonWarfare.Scenes.World.Service.Chat`.

---

## Coding conventions

**Formatting** (`.editorconfig`): UTF-8, LF, maximum line length of 120 characters.

**Initialization**

* The first line of `_Ready()` (or of a non-node constructor) is `Di.Process(this)`.
* If a node needs data **before** `_Ready()`, use the `InitPreReady(...)` method, which returns `this`:
  `PackedScene.Instantiate<Hud>().InitPreReady(world)`. For initialization after readiness — `InitPostReady(...)`.
* Heavy top-level initialization is split into `Init()` and `Start()`.

**Client and server**

* Check the role only via `Net.*`, not via `GetMultiplayer().IsServer()` directly.
* Methods valid only on one side start with a guard:

  ```csharp
  if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
  ```

* An `OnServer` / `OnClient` suffix on a method name denotes the execution side
  (`InitOnServer`, `StartSyncOnClient`, `RejectSyncOnClient`).
* Events are named `<What>Event` with the side indicated: `SaveSuccessServerEvent`, `SyncEndedOnClientEvent`.

**RPC**

* A public wrapper method + a private method with the `Rpc` suffix annotated with `[Rpc]`.
  The wrapper is a single line like `=> RpcId(ServerId, MethodName.XxxRpc, ...)` or `=> Rpc(...)`.
* Always specify the mode explicitly: `[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]` for
  "client → server", `[Rpc(CallLocal = true)]` for "server → client".
* For "chatty" streams, specify a `TransferChannel` from `Consts.TransferChannel`.
* Arguments are Godot primitives or a `byte[]` from MessagePack. JSON is not used over the network.
* Try to batch data into a single call (e.g. the coordinates of several units at once),
  rather than sending an RPC per object.

**Serialization**

* Network and saves — **MessagePack** (`[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]`).
* Settings files on disk — **JSON** (`System.Text.Json`), with a `ColorJsonConverter` for `Color`.

**Data**

* Data classes contain only state and its synchronization, with no game logic.
* Models are `ObservableObject` + `[ObservableProperty]`; the storage subscribes to `PropertyChanged`
  and broadcasts changes itself. Remember: assigning a property on the server generates network traffic.

**Logging** — Serilog via `[Logger] private ILogger _log`, with named template parameters:

```csharp
_log.Information("Connecting to the server at {host}:{port}", host, port);
```

**Precision: float vs double**

We follow vanilla Godot: time is stored in `double`, coordinates and angles in `float`.
For non-critical things (visuals, auxiliary calculations) we use `float` and simply cast `deltaTime`
from `double` to `float`. For critical ones (unit stats, damage, healing) — `double`.

**Strings**

* Error messages and templates — `private const string` at the top of the class, substituted via
  `FormatWith(...)` from Humanizer.
* Anything the player sees goes through `Services.I18N.Tr(KEY)` with keys in `SCREAMING_SNAKE_CASE`.

**Memory leaks**

Subscriptions to `MultiplayerApi` and service events are wrapped in local functions so they can be
correctly unsubscribed (`GetMultiplayer().ConnectedToServer -= ConnectedToServerEvent`). This is a common
cause of leaks on Game → MainMenu transitions, so such places in the code are accompanied by a comment.

---

## Localization

Translation files are `Assets/Locales/en.po` and `ru.po`; the template is `messages.pot`.
The current locale is set in `RootStarter` **after** loading settings but **before** the first show of the
loading screen. The default locale is taken from the OS (`Services.I18N.GetUserOsLocaleInfoOrDefault()`).
You can change the language on the `LanguageSelectionPage`.

## Debugging and performance

* `WorldPerformanceService` provides four sets of metrics shown right in the HUD:
  `Godot` (FPS, objects, draw calls), `Sharp` (.NET memory), `ENet` (traffic, loss, per-peer RTT),
  `Ping` (a custom ping mechanism from KludgeBox).
* The `Log` button in the HUD prints the entire node tree (`Services.NodeTree.LogFullTree(world)`).
* The `Test1` / `Test2` / `Test3` buttons in the HUD and ServerHud spawn bots — temporary, marked `//TODO`.
* The `--godot-log-push` flag mirrors Serilog logs to the Godot editor console.

## Current state

The project is in active development; some systems are scaffolded but not yet wired up:

* `NavigationService` and `Pathfinder` are written but used nowhere — pathfinding is not yet enabled in the world.
* `ClientPackedScenes` is empty — there are no purely client-side (visual) scenes yet.
* `BattleSurface` and `SafeSurface` do not differ from each other in logic yet.
* Skills and attacks are not implemented: the relevant `CharacterStat`s exist, but input handling is a stub.
* `ContextStorage` is a leftover of the previous menu version, replaced by `PagesProvider`.
* The code is sprinkled with `//TODO`s with open questions about spawn synchronization, teleporting, and splitting
  `CharacterController` into client and server parts.
