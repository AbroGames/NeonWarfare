# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Documentation

The architecture is documented in detail under `docs/` — that is the source of truth, not a retelling
of the code. Before a non-trivial task, read the relevant file in full:

| Topic | File |
|---|---|
| Layers, scene tree, the "calls go down, events go up" rule | `docs/arch/scene-tree.md` |
| Process startup flow | `docs/arch/startup-flow.md` |
| The 4 game session modes | `docs/arch/game-session-modes.md` |
| RPC, spawning, client handshake | `docs/arch/networking.md` |
| Persistence/Temporary data, saves | `docs/arch/data-and-saves.md` |
| `Character` and its subsystems | `docs/arch/entities.md` |
| Global services and world services | `docs/arch/services.md` |
| DI attributes | `docs/arch/dependency-injection.md` |
| Menu page stack, settings generation | `docs/arch/ui.md` |
| Chat and chat commands | `docs/arch/chat-and-commands.md` |
| Autosave on exit, killing child processes | `docs/arch/shutdown.md` |
| Code conventions (RPC, serialization, logging, float/double) | `docs/code-style.md` |
| All command-line flags | `docs/cli-args.md` |
| Localization: `.po` files, locale selection | `docs/localization.md` |
| Environment setup, Rider run configurations | `docs/quick-start.md` |

**When you change the architecture, update the corresponding file in `docs/`.** It is not generated
from the code.

## Commands

There are no tests in the repository — no test project, no framework. Changes are verified by
compiling plus a manual run of the game.

```bash
dotnet build                            # quick compile check (~3 s)
"$GODOT_EXE" --path "./"                # normal game launch
"$GODOT_EXE" --path "./" --auto-start   # straight into a singleplayer game, skipping the menu
"$GODOT_EXE" --path "./" --server       # dedicated server

# straight into a singleplayer game on a named save (created if it does not exist yet)
"$GODOT_EXE" --path "./" --auto-start --auto-start-savefile test

# client that auto-connects to a running server
"$GODOT_EXE" --path "./" --auto-connect --uid TestPlayer1 --nick TestPlayer1
```

The same scenarios exist as profiles in `Properties/launchSettings.json` (plus `Fast-test`
Multi-Launch configurations that bring up a server and one or two clients at once). When you edit one
set, keep the other in sync.

When verifying networking changes, always bring up a **server plus at least one client**: a
singleplayer game does not exercise the client-side code branches (see the `Net` semantics below).

**Build noise that does not need fixing:** `CS0649`, suppressed in the `.csproj` (those fields are
filled in by DI, not by a constructor).

## Architecture: what to understand before making changes

### One scene tree for both roles

Client and server are **the same code and the same scene tree**. The role is decided at runtime via
`Net.*`. There are no separate server-side scenes.

### `Net` semantics — the main source of mistakes

`Services.Net` (`Scripts/Service/NetworkService.cs`) overrides the default behaviour so that the roles
are **not mutually exclusive**:

* `Net.IsServer()` → `true` **always, except** when "we are connected as a client to someone else's
  server". So in the main menu and in a singleplayer game it is `true` — the process is its own
  authority.
* `Net.IsClient()` → `!isDedicatedServer`, so on a host **both** checks are true.

The practical consequence: server logic is written **once** and works in singleplayer as-is. Branch
with the helpers `Net.DoServerClient(...)`, `Net.DoClient(...)`, `Net.DoServerNotServer(...)` (for the
difference between the latter two, see `Character._Ready()`), not with `if`s on
`GetMultiplayer().IsServer()`.

### Two levels of starters

Startup is split across two levels, and they are not the same thing:

1. **RootStarter** — process level. `RootStarterManager` picks `ClientRootStarter` or
   `DedicatedServerRootStarter` based on whether `--server` is present in `OS.GetCmdlineArgs()`.
2. **GameStarter** — session level. `Services.MainScene.*` creates `Game` and hands it one of the four
   `BaseGameStarter`s, which knows whether to bring up an ENet server, an ENet client, or nothing.

**Command-line arguments are parsed only in the RootStarters** (`Scripts/Content/CmdArgs/`). From
there on they travel as ordinary parameters — do not read `OS.GetCmdlineArgs()` from deep in the code.

### Containers instead of direct references

Swapping content always goes through `NodeContainer` (`Scenes/KludgeBox/NodeContainer.cs`): `Root`
holds `MainSceneContainer` (MainMenu ↔ Game) and `LoadingScreenContainer`; `Game` holds
`WorldContainer` and `HudContainer` (Hud ↔ ServerHud). Scene prototypes come not from `GD.Load` but
from subclasses of `CheckedAbstractStorage` (`RootPackedScenes`, `GamePackedScenes`,
`SyncedPackedScenes`, `ClientPackedScenes`, `PagesProvider`) — their references are wired up in the
Godot editor.

Only scenes listed in `SyncedPackedScenes` can be spawned over the network.

### `World` as a service container

`World` implements `IServiceProvider` and registers its child services in `_EnterTree()` (before the
children's `_Ready()` runs) — that is what makes `[SceneService]` injection up the tree work.

A world service is the entry point for interacting with a system: its method must leave **all** state
consistent, not just its own. Logic lives in services, not in data classes and not in synchronizers.

### Writing to a data model is a network call

Models (`PlayerData`, `GeneralData`) are `ObservableObject`s with `[ObservableProperty]`. The storage
subscribes to `PropertyChanged` and broadcasts changes to clients itself, so **every property
assignment on the server generates traffic**. Do not use these properties as working variables in
loops.

`WorldPersistenceData` (goes into the save file, synced as a snapshot on connect) and
`WorldTemporaryData` (lives for the session, synced via `[Sync]`) are different nodes; choose
deliberately.

## Conventions that are easy to violate

The full list is in `docs/code-style.md`. The points where a mistake is not caught by the compiler:

* **`Di.Process(this)` is the first line** of `_Ready()` (or of the constructor for non-nodes).
  Without it, every `[Child]`/`[Parent]`/`[SceneService]`/`[Logger]` field stays `null`.
* **The `.cs.uid` next to each `.cs` is automatic** — Godot generates them; never create a `.cs.uid`
  file yourself.
* **A scene and its handler live in the same folder under the same name** (`Hud.tscn` + `Hud.cs`). The
  namespace mirrors the path: `Scenes/World/Service/Chat/WorldChatService.cs` →
  `NeonWarfare.Scenes.World.Service.Chat`.
* **There is only one entity network synchronizer — `CharacterSynchronizer`; it is split into
  `partial` files with `_`**: `CharacterSynchronizer_Stats.cs`.
* **An RPC is a pair: public wrapper + private `*Rpc` method**, with no blank line between them (that
  is deliberate). The mode is always stated explicitly. The payload is Godot primitives or `byte[]`
  from MessagePack; JSON never goes over the wire. Batch data into a single call instead of one RPC
  per object.
* **MessagePack for network and saves, `System.Text.Json` for settings files on disk.** Do not mix
  them up.
* **`double` for gameplay values** (stats, damage, healing, time), `float` for visuals and helper
  calculations.
* **Unsubscribing from `MultiplayerApi` and service events** is done via local functions — otherwise
  memory leaks on the Game → MainMenu transition. Such places carry a comment.
* **Everything the player can see goes through `Services.I18N.Tr(KEY)`**, with keys in
  `SCREAMING_SNAKE_CASE` and translations in `Assets/Locales/{en,ru}.po`. The translation template is
  `Assets/Locales/messages.pot`. Add new text to all three files.
* **New `global using`s go only in `Scripts/GlobalUsings.cs`.** That is also where `Di`, `Net`,
  `ServerId`, `BroadcastId` and the KludgeBox Godot extensions (`Vec2(x, y)` and friends) come from.
* **A new chat command** is just an `ICommandProcessor` implementation in
  `Scenes/World/Service/Command/Impl/`; registration is automatic via an assembly scan.
* **Formatting** (`.editorconfig`): UTF-8, LF, max line length 120 characters.

## Player files

`user://saves/<name>.bin` (MessagePack), `game-settings.json`, `dedicated-server-settings.json`,
`resume-game.json`. On Linux `user://` is `~/.local/share/godot/app_userdata/Neon Warfare/`.
