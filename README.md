# Neon Warfare

Neon Warfare is a co-op top-down bullet hell built on Godot and C#, where every session is short but
demands real coordination between players: the enemies in a fight are different every time, and the
tactics have to be worked out on the fly. The game is an indie project with a free version, and runs
on Windows/Linux/macOS.

---

## Documentation

### Foundation — read for any task

| Document | What is inside |
|---|---|
| [Scene tree](Docs/Scene-tree.md) | `NodeContainer`, "calls go down, events go up" |
| [Dependency injection](Docs/Dependency-injection.md) | `Di.Process(this)`, `[Child]`, `[Parent]`, `[SceneService]` |
| [Code style conventions](Docs/Code-style.md) | Namespaces, RPC pairs, serialization, `.editorconfig` |

### Area-by-area design — read when the task touches that area

| Document | Read when you touch |
|---|---|
| [Networking](Docs/Networking.md) | RPC, spawning, `Net.IsServer()` / `IsClient()`, `DoServerClient` |
| [Data and saves](Docs/Data-and-saves.md) | `Persistence` / `Temporary`, MessagePack save format |
| [Services](Docs/Services.md) | `Services.*`, global and world services |
| [Startup flow](Docs/Startup-flow.md) | `RootStarter`, `GameStarter`, the four session modes |
| [Shutdown](Docs/Shutdown.md) | Autosave on exit, killing child processes |
| [Entities](Docs/Entities.md) | `Character` and its subsystems: controllers, stats, status effects |
| [UI](Docs/Ui.md) | `PagesProvider` menu stack, `MenuGameSettings`, HUD |
| [Chat and commands](Docs/Chat-and-commands.md) | `IChatMessageInterceptor`, `ICommandProcessor` |
| [Localization](Docs/Localization.md) | `Tr(KEY)`, `Assets/Locales/*.po` |

### References and environment — consult as needed

| Document | What is inside |
|---|---|
| [Command-line arguments](Docs/Cli-args.md) | All flags, `Scripts/Content/CmdArgs/` |
| [Repository structure](Docs/Repository-structure.md) | What lives in each folder |
| [Stack and dependencies](Docs/Stack.md) | Godot and .NET versions, libraries, `KLUDGEBOX_SRC` |
| [Quick start](Docs/Quick-start.md) | Environment setup, Rider run profiles |
| [Testing](Docs/Testing.md) | The approach to tests, `dotnet test`, what is covered |
| [Smoke testing](Docs/Smoke-testing.md) | Running the real game from a test, `GODOT_EXE` |

---

## What to read for a task

| Task | What to read |
|---|---|
| Add or change an RPC | [Networking](Docs/Networking.md) → [Conventions](Docs/Code-style.md#rpc) |
| Add a field that goes into the save | [Data and saves](Docs/Data-and-saves.md) → [Networking](Docs/Networking.md) (the initial world snapshot) |
| Add a world service | [Services](Docs/Services.md) → [Scene tree](Docs/Scene-tree.md) (node naming without the `World` prefix) → [DI](Docs/Dependency-injection.md) |
| Add a game session mode | [Startup flow](Docs/Startup-flow.md) → [Networking](Docs/Networking.md) (process roles) |
| Add a command-line flag | [Command-line arguments](Docs/Cli-args.md) → [Startup flow](Docs/Startup-flow.md) |
| Add a chat command | [Chat and commands](Docs/Chat-and-commands.md) |
| Add a menu page or a setting | [UI](Docs/Ui.md) → [Localization](Docs/Localization.md) |
| Add a stat, a status effect, a character subsystem | [Entities](Docs/Entities.md) → [Networking](Docs/Networking.md) |
| Add player-visible text | [Localization](Docs/Localization.md) |
| The code behaves differently in single-player and over the network | [Networking](Docs/Networking.md) (`IsServer` / `IsClient`) → [Startup flow](Docs/Startup-flow.md) |
| Add a test or figure out what is tested at all | [Testing](Docs/Testing.md) → [Smoke testing](Docs/Smoke-testing.md) |
| `[Child]` / `[SceneService]` came out `null` | [DI](Docs/Dependency-injection.md) → [Scene tree](Docs/Scene-tree.md) |

---

## Code entry points

| Path | What it is |
|---|---|
| [Scenes/Root/Root.cs](Scenes/Root/Root.cs) | The process entry point, lives for the whole application session |
| [Scenes/Game/Game.cs](Scenes/Game/Game.cs) | A single game session: `Network`, `World`, `Hud` / `ServerHud`; created anew on every entry into the game |
| [Scenes/World/World.cs](Scenes/World/World.cs) | The world root, `IServiceProvider`, the world service registry |
| [Scenes/World/Data/PersistenceData/WorldPersistenceData.cs](Scenes/World/Data/PersistenceData/WorldPersistenceData.cs) | Data that goes into the save |
| [Scenes/World/Data/TemporaryData/WorldTemporaryData.cs](Scenes/World/Data/TemporaryData/WorldTemporaryData.cs) | Data of the current session, does not go into the save |
| [Scenes/World/Tree/WorldTree.cs](Scenes/World/Tree/WorldTree.cs) | The game tree, switching locations |
| [Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs](Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs) | The peaceful hub — the first of the two locations |
| [Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs](Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs) | The battle zone — the second of the two locations |
| [Scenes/Entity/Characters/Character.cs](Scenes/Entity/Characters/Character.cs) | The character (`RigidBody2D`) and all of its subsystems |
| [Scripts/Services.cs](Scripts/Services.cs) | The global service registry |
| [Scripts/Consts.cs](Scripts/Consts.cs) | Global constants, `Consts.TransferChannel` |
| [Scripts/Content/CmdArgs/](Scripts/Content/CmdArgs/) | `CommonArgs`, `ClientArgs`, `DedicatedServerArgs` |
| [Properties/launchSettings.json](Properties/launchSettings.json), [.run/](.run/) | Rider and Multi-Launch run profiles |
