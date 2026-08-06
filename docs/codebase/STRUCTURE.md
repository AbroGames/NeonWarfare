# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

| Path | Purpose | Evidence |
|------|---------|----------|
| `project.godot` | Godot project config: main scene (`Scenes/Root/Root.tscn`), physics, input maps, locales, renderer | `project.godot:18,65-77` |
| `NeonWarfare.csproj` / `.sln` | .NET build: `Godot.NET.Sdk 4.7.1`, `net10.0`, 3 NuGet packages, `PreBuild` dir scaffold | `NeonWarfare.csproj` |
| `README.md` | Authoritative (Russian) design doc: stack, CLI args, architecture, conventions, status | `README.md` |
| `Assets/` | Static resources: fonts (`Play`, `ProstoOne`), locales (`en.po`, `ru.po`, `messages.pot`), UI theme, shaders, textures (MainMenu, achievements, icons) | scan §DIRECTORY TREE; `project.godot:65` |
| `Scenes/` | Scene files (`.tscn`) **and their handlers (`.cs) sitting together in the same folder** — the core layout rule | `README.md:420` |
| `Scenes/Root/` | Application entry point + process starters (`RootStarterManager`, `BaseRootStarter`, `ClientRootStarter`, `DedicatedServerRootStarter`) | `Root.tscn:1`, `RootStarterManager.cs:29-34` |
| `Scenes/Game/` | One game session: `Game` node, `Network` wrapper, 4 game starters (singleplayer/host/connect/dedicated) | `Game.cs:10`, `Network.cs:7` |
| `Scenes/World/` | The world: `World` (`IServiceProvider`), `WorldTree` + surfaces, persistence/temporary data, all `World*Service`s | `World.cs:29,56-80` |
| `Scenes/Entity/` | Game objects: `Characters/` (controller, stats, status effects, synchronizer, AI) and `Walls/` (scriptless `StaticBody2D`) | `Character.cs:11`, `Wall.tscn:19` |
| `Scenes/Screen/` | UI: `NewMenu/` (page stack system), `Hud/`, `ServerHud/`, `LoadingScreen/` | `PageContainer.cs:7`, `Hud.cs:8` |
| `Scenes/KludgeBox/` | Thin project-side subclasses of KludgeBox nodes (e.g. `CheckedAbstractStorage` wrapper) | `CheckedAbstractStorage.cs:6` |
| `Scripts/` | Code not attached to a scene: `Content/CmdArgs/`, `Service/` (global services, settings, save-load), `Services.cs`, `Consts.cs`, `GlobalUsings.cs` | `Services.cs:13`, `Consts.cs:6` |
| `bin/` | Build output (gitignored artifacts); `.gdignore` placed inside so Godot skips it | `NeonWarfare.csproj:17-25` |
| `.godot/` | Godot editor cache/imported assets — never edit, never document patterns from here | scan §DIRECTORY TREE |
| `.idea/` | JetBrains IDE config | scan §DIRECTORY TREE |
| `.github/` | Present but no CI workflows detected by scan | scan §CI/CD ("No CI/CD pipelines detected") |
| `*.csproj.old*` | Old `.csproj` backups (5 files) — version history, not active build files | scan §DIRECTORY TREE |

### 2) Entry Points

- **Main runtime entry:** `Scenes/Root/Root.tscn` (UID `uid://bjyux48ai45ry`, set in `project.godot:18`). Its script `Root.cs` is the top-level node.
- **Process-mode selection:** `Root._Ready()` → `Di.Process(this)` → deferred `Init()`+`Start()` → `RootStarterManager.ChooseStarter()` picks `DedicatedServerRootStarter` if `--server` is in `OS.GetCmdlineArgs()`, else `ClientRootStarter` (`RootStarterManager.cs:29-34`).
- **C# code entry:** There is no `static Main` — Godot hosts the .NET assembly `NeonWarfare` (`project.godot:24`). Execution begins at Godot's main scene lifecycle (`Root._Ready()`).
- **Secondary entry points:** Dedicated server is launched as a **separate child process** with `--server` by `ProcessService.StartNewDedicatedServerApplication(...)` (`ProcessService.cs:14-29`); the child's argv is rebuilt by `DedicatedServerArgs.GetArrayToStartDedicatedServer()` (`DedicatedServerArgs.cs:39-55`).

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|-------------------|------------------------|
| `Scenes/Root/` | App entry, process-startup orchestration (one-time wiring) | Game/world logic, persistent services |
| `Scenes/Game/` | One game session: Network setup, world/HUD instantiation, game-mode starters | Per-frame game logic (lives in World/Entity) |
| `Scenes/World/Service/` | World-scoped services; each is "a point of interaction with the system" that must propagate consistent state (`World.cs:24-28`) | Game-object internals (Entity owns those) |
| `Scenes/World/Data/` | Pure state + its network/disk synchronization; **no game logic** (`WorldPersistenceData.cs:8-11`) | Damage/heal/movement rules |
| `Scenes/Entity/` | Game objects and their subsystems (controller/stats/effects) | Cross-entity orchestration (a World service's job) |
| `Scenes/Entity/Characters/Synchronizer/` | **Network logic only** (`CharacterSynchronizer_Stats.cs:18` TODO: "no logic except network here") | Gameplay rules, stat math |
| `Scenes/Screen/` | UI presentation (menu pages, HUD, loading) | Authoritative game state |
| `Scripts/Service/` | Global singletons (settings, save-load, main-scene switching) accessed via `Services` | Scene-tree-coupled per-world logic |
| `Scripts/Content/CmdArgs/` | CLI argument definitions; **parsed only in RootStarters** then passed as plain params (`README.md:72-73`) | Global CLI reads from services |

### 4) Naming and Organization Rules

- **Scene + handler co-location:** A scene `.tscn` and its handler `.cs` live in the same folder and share the base name: `Hud.tscn` + `Hud.cs`, `Character.tscn` + `Character.cs` (`README.md:420`).
- **Partial-class splitting for large handlers:** Suffix `_Subsystem` (underscore): `CharacterSynchronizer.cs`, `CharacterSynchronizer_Stats.cs`, `CharacterSynchronizer_Controller.cs`, `CharacterSynchronizer_StatusEffects.cs` (`README.md:421-422`).
- **Namespaces mirror file paths:** `Scenes/World/Service/Chat/WorldChatService.cs` → `NeonWarfare.Scenes.World.Service.Chat` (`README.md:424-425`).
- **PascalCase** for files, types, public members. **`SCREAMING_SNAKE_CASE`** for i18n keys (`Assets/Locales/*.po`) and error-message constants (`private const string`).
- **Method-side suffixes (intent-encoded):** `OnServer`/`OnClient` = execution side; `*Rpc` = private RPC receiver paired with a public wrapper; `<Thing>Event` = an event/signal with side in the name (e.g. `SyncEndedOnClientEvent`).
- **Global usings centralized:** `Scripts/GlobalUsings.cs` is the only place to add `global using` directives; it static-imports `Services.Global` (`Di`, `Net`), `Consts.Global` (`ServerId`, `BroadcastId`), and KludgeBox extension classes (`Vec2`, etc.) (`GlobalUsings.cs:1-12`).
- **`Services` registry:** all global singletons live in `Scripts/Services.cs`; world-scoped services live under `Scenes/World/Service/`.

### 5) Evidence

- `docs/codebase/.codebase-scan.txt` §DIRECTORY TREE — top-level + 3-level tree.
- `project.godot:18,65-77` — main scene, locales, physics.
- `Scenes/Root/Root.tscn:1`, `Scenes/Root/Root.cs:16-24` — entry node and lifecycle.
- `Scenes/Root/Starters/RootStarterManager.cs:29-34` — starter selection.
- `Scenes/World/World.cs:29,56-80` — `World` as `IServiceProvider` + service registration.
- `Scenes/Entity/Characters/Synchronizer/CharacterSynchronizer_Stats.cs:18` — synchronizer boundary.
- `README.md:420-425` — co-location and namespace rules.

## Extended Sections (Optional)

### World Service Subdirectory Map

`Scenes/World/Service/` subfolders, each a cohesive domain:

| Subfolder | Contents |
|-----------|----------|
| `StartStop/` | `WorldServerStartStopService`, `WorldClientStartStopService`, `WorldServerShutdowner` |
| `Characters/` | `WorldCharacterService` (base), `WorldPlayerService`, `WorldEnemyService` |
| `DataSerializer/` | `WorldDataSerializerService`, `ISerializableStorage` |
| `Chat/` | `WorldChatService`, `ChatMessage`, `IChatMessageInterceptor` |
| `Command/` (+ `Impl/`) | `WorldCommandService`, `ChatMessageCommandInterceptor`, `ICommandProcessor` + 7 command impls |
| `Performance/` | `WorldPerformanceService` + `WorldGodotPerformance`, `WorldSharpPerformance`, `WorldENetPerformance`, `WorldPingPerformance` |
| `MpSpawn/` | `WorldMultiplayerSpawner` (project subclass of KludgeBox `AbstractMultiplayerSpawner`) |
| (root) | `WorldMultiplayerSpawnerService`, `WorldSynchronizerService`, `WorldDataSaveLoadService`, `WorldFacadeService`, `NavigationService` (dead code) |
