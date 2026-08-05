# Neon Warfare

Neon Warfare is a co-op top-down bullet hell built on Godot and C#, where every session is short but
demands real coordination between players: the enemies in a fight are different every time, and the
tactics have to be worked out on the fly. The game is an indie project with a free version, and runs
on Windows/Linux/macOS.

---

## Documentation

**If you change the architecture, update the corresponding file in `Docs/`.** It is not generated
from the code. If the change affects the list of documents or the entry points — update `README.md`
as well. Keep doc changes as compact as possible.

Before a non-trivial task, the file you need is read **in full**.  
Key entities are named in the table rows deliberately
so that a file can be found by searching for a class or attribute name.

### Foundation — read for any task

| Document | What is inside |
|---|---|
| [Scene tree](Docs/Scene-tree.md) | The "container → contents" hierarchy, `NodeContainer`, the "calls go down, events go up" rule, node naming |
| [Dependency injection](Docs/Dependency-injection.md) | `Di.Process(this)`, `[Child]`, `[Parent]`, `[SceneService]`, `[Logger]` |
| [Code style conventions](Docs/Code-style.md) | Namespaces, RPC pairs, serialization, logging, `double`/`float`, `.editorconfig` |

### Area-by-area design — read when the task touches that area

| Document | Read when you touch |
|---|---|
| [Networking](Docs/Networking.md) | RPC, spawning, synchronization, the client handshake, `Net.IsServer()` / `IsClient()`, `DoServerClient` |
| [Data and saves](Docs/Data-and-saves.md) | `Persistence` / `Temporary`, the save format, MessagePack, paths on Windows and Linux |
| [Services](Docs/Services.md) | Global services and world services, `Services.*` |
| [Startup flow](Docs/Startup-flow.md) | `RootStarter`, `GameStarter`, the four game session modes |
| [Shutdown](Docs/Shutdown.md) | Autosave on exit, killing child processes |
| [Entities](Docs/Entities.md) | `Character` and its subsystems: controllers, stats, status effects |
| [UI](Docs/Ui.md) | The menu page stack, `PagesProvider`, the menu pages, `MenuGameSettings` settings generation, HUD, loading screen |
| [Chat and commands](Docs/Chat-and-commands.md) | Chat, `IChatMessageInterceptor` interceptors, `ICommandProcessor` chat commands |
| [Localization](Docs/Localization.md) | Player-visible text: `Tr(KEY)`, `Assets/Locales/*.po`, `messages.pot`, locale selection |

### References and environment — consult as needed

| Document | What is inside |
|---|---|
| [Command-line arguments](Docs/Cli-args.md) | All flags, `Scripts/Content/CmdArgs/` |
| [Repository structure](Docs/Repository-structure.md) | What lives in each folder |
| [Stack and dependencies](Docs/Stack.md) | Godot and .NET versions, libraries, where to look for the KludgeBox sources |
| [Quick start](Docs/Quick-start.md) | Environment setup, Rider run profiles |
| [Testing](Docs/Testing.md) | The approach to tests, `dotnet test`, what is already covered |
| [Smoke testing](Docs/Smoke-testing.md) | Launching the real game from a test, `GODOT_EXE`, what counts as a failure |

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
| `[Child]` / `[SceneService]` came out `null` | [DI](Docs/Dependency-injection.md) → [Scene tree](Docs/Scene-tree.md) |
| The code behaves differently in single-player and over the network | [Networking](Docs/Networking.md) (`IsServer` / `IsClient`) → [Startup flow](Docs/Startup-flow.md) |
| A type declaration cannot be found (`NodeContainer`, `[Sync]`, `StatModifiersContainer<T>`) | [Stack](Docs/Stack.md) — the KludgeBox sources are not in the repository, the path to them is in the `KLUDGEBOX_SRC` ENV variable |
| Bring up a server and a client to check something | [Quick start](Docs/Quick-start.md) → [Command-line arguments](Docs/Cli-args.md) |
| Add a test or figure out what is tested at all | [Testing](Docs/Testing.md) → [Smoke testing](Docs/Smoke-testing.md) |

---

## Code entry points

| Path | What it is |
|---|---|
| [Scenes/Root/Root.cs](Scenes/Root/Root.cs) | The process entry point, lives for the whole application session |
| [Scenes/Game/Game.cs](Scenes/Game/Game.cs) | A single game session: `Network`, `World`, `Hud` / `ServerHud`; created anew on every entry into the game |
| [Scenes/Game/Starters/](Scenes/Game/Starters/) | The four game session starters |
| [Scenes/World/World.cs](Scenes/World/World.cs) | The world root, `IServiceProvider`, the world service registry |
| [Scenes/World/Service/](Scenes/World/Service/) | World services |
| [Scenes/World/Data/PersistenceData/WorldPersistenceData.cs](Scenes/World/Data/PersistenceData/WorldPersistenceData.cs) | Data that goes into the save: `GeneralDataStorage`, `PlayerDataStorage` |
| [Scenes/World/Data/TemporaryData/WorldTemporaryData.cs](Scenes/World/Data/TemporaryData/WorldTemporaryData.cs) | Data of the current session, does not go into the save, synchronized via `[Sync]` |
| [Scenes/World/Tree/WorldTree.cs](Scenes/World/Tree/WorldTree.cs) | The game tree, switching locations: `SetSafeSurface()` / `SetBattleSurface()` |
| [Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs](Scenes/World/Tree/Surfaces/Safe/SafeSurface.cs) | The peaceful hub — the first of the two main locations |
| [Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs](Scenes/World/Tree/Surfaces/Battle/BattleSurface.cs) | The battle zone — the second of the two main locations |
| [Scenes/Entity/Characters/Character.cs](Scenes/Entity/Characters/Character.cs) | The character (`RigidBody2D`) and all of its subsystems |
| [Scripts/Services.cs](Scripts/Services.cs) | The global service registry |
| [Scripts/Consts.cs](Scripts/Consts.cs) | Global constants, `Consts.TransferChannel` |
| [Scripts/Content/CmdArgs/](Scripts/Content/CmdArgs/) | `CommonArgs`, `ClientArgs`, `DedicatedServerArgs` |
| [Properties/launchSettings.json](Properties/launchSettings.json), [.run/](.run/) | Rider and Multi-Launch run profiles |
