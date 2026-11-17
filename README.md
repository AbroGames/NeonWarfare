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
| [Dependency injection](Docs/Dependency-injection.md) | `Di.Process(this)`, `[Child]` |
| [Code style conventions](Docs/Code-style.md) | Namespaces, initialization, serialization, `.editorconfig` |

### Area-by-area design — read when the task touches that area

| Document | Read when you touch |
|---|---|
| [Networking](Docs/Networking.md) | `Network`, `ServerTickLoop`, commands, packets, join, `NetId`, replication |
| [World](Docs/World.md) | `World`, `WorldLayer`, the layer attributes, `WorldServicesBuilder`, the `Infra/` folders |
| [World features](Docs/World-features.md) | `Worlds/Features/`: what each feature holds, how it plugs in |
| [Data and saves](Docs/Data-and-saves.md) | `[Replicated]` models, storages, `[NotSaved]`, RepliCAT saves |
| [Services](Docs/Services.md) | `Services.*`, the global services |
| [Startup flow](Docs/Startup-flow.md) | `RootStarter`, `GameStarter`, the four session modes |
| [Shutdown](Docs/Shutdown.md) | Autosave on exit, killing child processes |
| [UI](Docs/Ui.md) | `PagesProvider` menu stack, `MenuGameSettings`, HUD |
| [Chat and commands](Docs/Chat-and-commands.md) | `ChatSimulationFacade`, `ChatPresentation`, `IChatCommand` |
| [Localization](Docs/Localization.md) | `Tr(KEY)`, `Assets/Locales/*.po` |

### References and environment — consult as needed

| Document | What is inside |
|---|---|
| [Command-line arguments](Docs/Cli-args.md) | All flags, `Src/NeonWarfare/Scripts/Content/CmdArgs/` |
| [Repository structure](Docs/Repository-structure.md) | What lives in each folder |
| [Stack and dependencies](Docs/Stack.md) | Godot and .NET versions, libraries, `KLUDGEBOX_SRC` |
| [Quick start](Docs/Quick-start.md) | Environment setup, Rider run profiles |
| [Testing](Docs/Testing.md) | The approach to tests, `dotnet test`, what is covered |
| [Smoke testing](Docs/Smoke-testing.md) | Running the real game from a test, `GODOT_EXE` |
| [Game testing](Docs/Game-testing.md) | Testing `Node` descendants inside the engine, gdUnit4Net, `GODOT_BIN` |

---

## What to read for a task

| Task | What to read |
|---|---|
| Add a command (client → server) | [World features](Docs/World-features.md) → [Networking](Docs/Networking.md) |
| Add an event (server → client) | [World features](Docs/World-features.md) → [Networking](Docs/Networking.md) |
| Add a field that goes into the save | [Data and saves](Docs/Data-and-saves.md) → [World features](Docs/World-features.md) |
| Add a world service | [World](Docs/World.md) (layers) → [World features](Docs/World-features.md) |
| Add a game session mode | [Startup flow](Docs/Startup-flow.md) → [World](Docs/World.md) (layer sets) |
| Add a command-line flag | [Command-line arguments](Docs/Cli-args.md) → [Startup flow](Docs/Startup-flow.md) |
| Add a chat command | [Chat and commands](Docs/Chat-and-commands.md) |
| Add a menu page or a setting | [UI](Docs/Ui.md) → [Localization](Docs/Localization.md) |
| Add player-visible text | [Localization](Docs/Localization.md) |
| The code behaves differently in single-player and over the network | [World](Docs/World.md) (layers) → [Networking](Docs/Networking.md) (the host's loopback) |
| Add a test or figure out what is tested at all | [Testing](Docs/Testing.md) → [Smoke testing](Docs/Smoke-testing.md) → [Game testing](Docs/Game-testing.md) |
| `[Child]` came out `null` | [DI](Docs/Dependency-injection.md) → [Scene tree](Docs/Scene-tree.md) |

---

## Code entry points

| Path | What it is |
|---|---|
| [Src/NeonWarfare/Scenes/Root/Root.cs](Src/NeonWarfare/Scenes/Root/Root.cs) | The process entry point, lives for the whole application session |
| [Src/NeonWarfare/Scenes/Game/Game.cs](Src/NeonWarfare/Scenes/Game/Game.cs) | A single game session: `Network`, `World`, `Hud` / `ServerHud`; created anew on every entry into the game |
| [Src/NeonWarfare/Scenes/Worlds/World.cs](Src/NeonWarfare/Scenes/Worlds/World.cs) | The World root: its entry points, built from a layer set |
| [Src/NeonWarfare/Scenes/Worlds/WorldServicesBuilder.cs](Src/NeonWarfare/Scenes/Worlds/WorldServicesBuilder.cs) | The container of the world services, found by their layer attributes |
| [Src/NeonWarfare/Scenes/Worlds/Infra/Composition/WorldLayer.cs](Src/NeonWarfare/Scenes/Worlds/Infra/Composition/WorldLayer.cs) | The layers and the client / host / dedicated sets |
| [Src/NeonWarfare/Scenes/Worlds/Features/](Src/NeonWarfare/Scenes/Worlds/Features/) | The game itself, one folder per feature |
| [Src/NeonWarfare/Scripts/Services.cs](Src/NeonWarfare/Scripts/Services.cs) | The global service registry |
| [Src/NeonWarfare/Scripts/Consts.cs](Src/NeonWarfare/Scripts/Consts.cs) | Global constants, `Consts.TransferChannel` |
| [Src/NeonWarfare/Scripts/Content/CmdArgs/](Src/NeonWarfare/Scripts/Content/CmdArgs/) | `CommonArgs`, `ClientArgs`, `DedicatedServerArgs` |
| [Src/GodotBox/](Src/GodotBox/) | The game-independent layer over KludgeBox: `NodeContainer`, storages, camera, process nodes |
| [Properties/launchSettings.json](Properties/launchSettings.json), [.run/](.run/) | Rider and Multi-Launch run profiles |
