# Architecture

## Core Sections (Required)

### 1) Architectural Style

- **Primary style:** Hierarchical **container → content** node tree with a **service-layer + DI** spine, single authoritative-server multiplayer.
- **Why this classification:** The project is a Godot game built as a strict parent→child scene hierarchy. Each level knows only its descendants; content is swapped through `NodeContainer` wrappers (`Root.tscn:13-17`). A static `Services` facade + KludgeBox `Di` attribute-injection provide cross-cutting services, and `World` itself implements `IServiceProvider` for world-scoped services (`World.cs:29`). Networking is Godot's high-level `SceneMultiplayer` + `ENetMultiplayerPeer` in an authoritative-server model (`Network.cs:7,24-25`).
- **Primary constraints:**
  1. **Control flow goes DOWN the tree, events/signals go UP** (`README.md:144-145`). A parent knows its children; a child does **not** know its parent (the only exception is explicit `[Parent]` injection in two world services).
  2. **Client and server share one scene tree** — role is decided at runtime via `Net.IsServer()`/`IsClient()` and `Net.DoServerClient(...)`/`DoServerNotServer(...)` (`README.md:140-142`, `Character.cs:35-46`).
  3. **One authoritative logic path:** singleplayer, host, and dedicated server all run "server" logic. `Net.IsServer()` returns `true` for singleplayer and main-menu (process is its own authority); it returns `false` **only** when connected as a client to a foreign server (`NetworkService.cs:33-42`).

### 2) System Flow

```text
[Godot loads Root.tscn]
   -> Root._Ready()  (Di.Process; deferred Init()+Start())
   -> RootStarterManager.ChooseStarter()  (--server ? DedicatedServer : Client)
   -> BaseRootStarter.Init()  (exception handler, assembly cache, TypesMapping,
                               LoadingScreen/MainScene/I18N services; then Net.Init,
                               settings, locale, autoscaling)
   -> Starter.Start()  -> Services.MainScene.<mode>(...)  (creates Game)
        -> Game.Init(BaseGameStarter)  -> AddNetwork / AddWorld / AddHud
           -> ServerStartWorld: StartNewGame|LoadGame  ...or...  ClientStartWorld: StartSyncWithServer
        -> World._EnterTree(): registers Tree, PersistenceData, TemporaryData, all World*Services
        -> Per-frame: World services + Character subsystems + Controllers (physics @ 30Hz)
        -> Exit: WorldServerShutdowner (autosave) / Network.Shutdown / MainScene.Shutdown(Quit)
```

Step-by-step (file-backed):

1. **Entry:** Godot loads `Root.tscn` (UID in `project.godot:18`). `Root._Ready()` runs `Di.Process(this)` then defers `Init()`+`Start()` (`Root.cs:16-24`).
2. **Starter selection:** `RootStarterManager.ChooseStarter()` checks `OS.GetCmdlineArgs()` for `--server` (`RootStarterManager.cs:29-34`).
3. **Base wiring:** `BaseRootStarter.Init()` wires exception handling, assembly/type scanning, `LoadingScreen`, `MainScene`, `I18N`; subclasses add `Services.Net.Init(isServer)`, settings, locale, autoscaling (`BaseRootStarter.cs:21-37`, `ClientRootStarter.cs:14-34`, `DedicatedServerRootStarter.cs:13-28`).
4. **Game session:** `Starter.Start()` calls a `Services.MainScene.*` method which instantiates `Game` and hands it a `BaseGameStarter`; `Game.Init()` delegates to the starter which calls `AddNetwork/AddWorld/AddHud` and either `ServerStartWorld` or `ClientStartWorld` (`Game.cs:24-61`, `BaseGameStarter.cs:28-52`).
5. **World boot:** `World._EnterTree()` runs `Di.Process(this)` then registers every child node (data + services) into a `Dictionary<Type,object>`, enabling `[SceneService]` injection (`World.cs:56-80`).
6. **Per-frame loop:** `_PhysicsProcess` (30 Hz) drives stats regen, status-effect ticks, and `CharacterController.OnPhysicsProcess/OnIntegrateForces`; `_UnhandledInput` routes to the controller (`Character.cs:52-70`, `PhysicsCalculator.cs:30-60`).
7. **Client join handshake:** client `StartSyncOnClient` → server `NewClientInitOnServerRpc` (validates uid/nick/color) → `EndSyncOnClientRpc(byte[])` (whole `PersistenceData` as MessagePack) → client deserializes → server `SpawnPlayer(peerId)` (`WorldSynchronizerService.cs:64-113`).
8. **Shutdown:** `WorldServerShutdowner` catches `NotificationExitTree` → autosave; `Network.Shutdown()` (also on `NotificationExitTree`) tears down the peer; `MainSceneService.Shutdown()` deferred `Quit()` (`WorldServerShutdowner.cs:32-42`, `Network.cs:115-142`, `MainSceneService.cs:112-117`).

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| `Root` + RootStarters | Process-mode selection, one-time global wiring, top-level Init/Start lifecycle | Game/world per-frame logic | `Root.cs:16-36`, `BaseRootStarter.cs:21-42` |
| `Game` + game starters | One session: network bring-up, World/HUD instantiation, new-game/load/connect orchestration | Stat math, AI | `Game.cs:24-61`, `BaseGameStarter.cs:28-52` |
| `Network` / `NetworkStateMachine` | `SceneMultiplayer` lifecycle scoped to Game node, connect/host/open/shutdown, RPC plumbing | Game state | `Network.cs:7,17-34,43-142`, `NetworkStateMachine.cs:7-26` |
| `World` (+ `World*Service`) | World services — each a consistent-state interaction point (`World.cs:24-28`) | Entity internals | `World.cs:29,56-80` |
| `World/Data` | State + serialization/sync only | Game logic | `WorldPersistenceData.cs:8-11`, `WorldTemporaryData.cs:17` |
| `Entity/Character` subsystems | Character behaviour split into server/client pairs (Stats, StatusEffects, Controller) | Cross-entity orchestration | `Character.cs:11,35-49` |
| `CharacterSynchronizer` | **Network glue only** between a Character's server and client halves | Any gameplay logic | `CharacterSynchronizer_Stats.cs:18` |
| `Scripts/Service` | Global singletons (settings, save-load, main-scene switching) accessed via `Services` | Scene-tree-coupled per-world logic | `Services.cs:13-40`, `MainSceneService.cs` |
| `Screen/` | UI presentation | Authoritative state | `PageContainer.cs:7`, `Hud.cs:8` |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|---------------|
| **Container → content** with `NodeContainer` swap | `Root`, `Game` (`Root.tscn:13-17`, `Game.cs:29-53`) | Lets MainMenu↔Game and Hud↔ServerHud swap cleanly; guarantees old multiplayer subscriptions die on teardown |
| **Static `Services` facade + eager singletons** | `Scripts/Services.cs:16-40` | Global access (any code can call `Services.MainScene...`) without prop-drilling through the node tree |
| **Attribute DI via KludgeBox `Di.Process(this)`** | ~50 call sites; `[Child]`×89, `[SceneService]`×47, `[Logger]`×17, `[NotNull]`×25, `[Parent]`×2 | Decouples wiring; first line of `_Ready()`/constructors |
| **`World` as `IServiceProvider`** | `World.cs:29,82-96` | Enables `[SceneService]` to resolve sibling services by type from the nearest provider up the tree |
| **Server/client subsystem split** | `CharacterStats`/`CharacterStatsClient`, `CharacterStatusEffects`/`...Client`, controller variants (`Character.cs:35-46`) | One scene tree, role picked at runtime; server authoritative, client mirrors |
| **Public RPC wrapper + private `*Rpc` receiver** | `WorldChatService.cs:27-50`, `WorldSynchronizerService.cs:64-113`, `CharacterSynchronizer_Controller.cs:69-75` | Keeps the send API type-safe and centralizes `[Rpc]` attributes |
| **MessagePack `byte[]` payloads** | `IController.cs:11` (`MovementData`), `ChatMessage.cs:6`, world snapshot (`WorldSynchronizerService.cs:111-113`) | Compact binary for both network and saves; primitives or `byte[]` only (no JSON over network) |
| **`ObservableProperty` → `PropertyChanged` → RPC broadcast** | `GeneralDataStorage.cs:33-42`, `PlayerDataStorage.cs:39-56` | A model property write on the server **is inherently networked** — storages subscribe and push deltas |
| **`CheckedAbstractStorage` for editor-wired `PackedScene`s** | `RootPackedScenes`, `GamePackedScenes`, `SyncedPackedScenes`, `PagesProvider` | Validates `[Export][NotNull]` references set in the editor at startup |
| **Page-stack menu** | `PageContainer`/`Page`/`MainMenuPage`/`PagesProvider` (`PageContainer.cs:19-94`) | Replaces older `ContextStorage`; linked parent/child page chain with cycle detection |
| **Settings-from-model (reflection)** | `MenuGameSettings.GetVisibleSettings()` + `[Name]/[Hint]/[Hide]/[Range]/[Step]` (`GameSettingsBase.cs:33-40`) | UI controls generated from the model, not hand-built |

### 5) Known Architectural Risks

- **`NetworkService.IsServer()` is global and side-aware, not a pure peer check.** Because singleplayer and main-menu return `true`, any code assuming "`IsServer()` ⇒ there is a real network peer" is subtly wrong. Mitigated by the documented convention, but easy to misuse (`NetworkService.cs:33-42`).
- **`SceneMultiplayer` is re-scoped to the Game node on every `Network._Ready`.** This is intentional (clean teardown) but means multiplayer identity is tied to Game's lifetime — a Game→MainMenu→Game cycle relies on it being rebuilt correctly (`Network.cs:22-25`).
- **Shutdown ordering hazard (documented).** After `TreeExiting`, `GetMultiplayer()` is null and `Network` may have swapped in an `OfflineMultiplayerPeer`; a dedicated `WorldServerShutdowner` exists precisely because those cannot be trusted at that moment (`WorldServerShutdowner.cs:9-13`).
- **Authoritative server spawned as a child process** (`HostDedicatedServerAndConnectGameStarter`) introduces IPC-by-PID (`ProcessDeadChecker`/`ProcessShutdowner`) — fragile across OS process-tree changes (`HostDedicatedServerAndConnectGameStarter.cs:23-26`, `HostMultiplayerGameStarter.cs:27-34`).
- **Heavy reliance on reflection** for services discovery (`MembersScanner`), command auto-discovery (`WorldCommandService.cs:44-69`), and settings generation — robust today but a debugging/compat surface.

### 6) Evidence

- `Scenes/Root/Root.cs:16-36`, `Scenes/Root/Starters/RootStarterManager.cs:29-34` — entry + selection.
- `Scenes/Game/Game.cs:24-61`, `Scenes/Game/Network/Network.cs:7,17-34,43-142` — session + network lifecycle.
- `Scenes/World/World.cs:29,56-80,82-96` — `IServiceProvider` world + service registry.
- `Scenes/Entity/Characters/Character.cs:35-49` — server/client split.
- `Scenes/World/Service/WorldSynchronizerService.cs:64-113` — join handshake.
- `Scripts/Service/NetworkService.cs:23-42`, `Scripts/Services.cs:13-46` — role semantics + service facade.
- `README.md:107-398` — authored architecture description (cross-checked against code).

## Extended Sections (Optional)

### Startup / Initialization Order

```
Root._Ready()  ->  Di.Process(this)  ->  deferred:
  RootStarterManager.Init()  ->  chosen Starter.Init():
      BaseRootStarter.Init():  Di.Process(this)
        -> CommonArgs.GetFromCmd
        -> LogFactory.GodotLogPushEnable
        -> ExceptionHandler.AddExceptionHandlerForUnhandledException
        -> AssemblyCache.AddAssembly(GetExecutingAssembly())
        -> TypesMapping.AddTypes(...)
        -> LoadingScreen.Init / MainScene.Init / I18N.Init
      [Client]:  ClientArgs.GetFromCmd -> Net.Init(false) -> AutoScaling.Init
                 -> LastGame.Init -> GameSettings.Init (+ temp nick/uid) -> set locale
      [Server]:  DedicatedServerArgs.GetFromCmd -> Net.Init(true) -> LastGame.Init
                 -> DedicatedServerSettings.Init -> set locale -> window title "[SERVER] ..."
  RootStarterManager.Start()  ->  chosen Starter.Start():
      [Client]:  --auto-start -> StartSingleplayerGame(newSave)
               |  --auto-connect -> ConnectToMultiplayerGame(ip,port)
               |  else -> StartMainMenu() + LoadingScreen.Clear()
      [Server]:  HostMultiplayerGameAsDedicatedServer(saveFile, port, admin, parentPid, noHud, worldRender)
```
(`BaseRootStarter.cs:21-37`, `ClientRootStarter.cs:14-55`, `DedicatedServerRootStarter.cs:13-42`)

### Multiplayer Game-Mode Matrix

| Game starter | Network? | Host | Use case |
|---|---|---|---|
| `SingleplayerGameStarter` | None (no `Network`) | process is its own server | Singleplayer from menu, `--auto-start` |
| `HostMultiplayerGameStarter` | ENet server | same process | Host-in-client and dedicated server |
| `ConnectToMultiplayerGameStarter` | ENet client | remote process | Connect to server, `--auto-connect` |
| `HostDedicatedServerAndConnectGameStarter` | ENet client + child server process | separate process | Host with external server (spawns `--server` child, connects to it) |

(`SingleplayerGameStarter.cs`, `HostMultiplayerGameStarter.cs:36-63`, `ConnectToMultiplayerGameStarter.cs:21-43`, `HostDedicatedServerAndConnectGameStarter.cs:11-34`)
