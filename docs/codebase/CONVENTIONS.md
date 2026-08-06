# Coding Conventions

## Core Sections (Required)

### 1) Naming Rules

| Item | Rule | Example | Evidence |
|------|------|---------|----------|
| Files (C#) | Match the contained type, PascalCase; scene `.tscn` + handler `.cs` co-located, same base name | `Hud.tscn` + `Hud.cs`; `WorldChatService.cs` | `README.md:420`; `Scenes/Screen/Hud/` |
| Partial-class files | `<Class>_<Subsystem>.cs` (underscore suffix) for splitting large handlers | `CharacterSynchronizer_Stats.cs`, `CharacterSynchronizer_Controller.cs` | `README.md:421-422`; `Scenes/Entity/Characters/Synchronizer/` |
| Types/interfaces | PascalCase; interfaces prefixed `I` | `IController`, `ICommandProcessor`, `IChatMessageInterceptor`, `IAiControllerLogic` | `IController.cs:11`, `ICommandProcessor.cs:3` |
| Namespaces | Mirror the file path under `NeonWarfare.` | `NeonWarfare.Scenes.World.Service.Chat` for `Scenes/World/Service/Chat/...` | `README.md:424-425` |
| Methods — side intent | `*OnServer`/`*OnClient` suffix = execution side | `InitOnServer`, `StartSyncOnClient`, `RejectSyncOnClient` | `WorldSynchronizerService.cs:64-113` |
| Methods — RPC | Public wrapper + private `<Name>Rpc` receiver | `Save(...)` → `SaveRpc(...)`; `Controller_SendMovement` → `Controller_SendMovementRpc` | `WorldChatService.cs:27-28`; `CharacterSynchronizer_Controller.cs:69-70` |
| Events | `<Thing>Event`, include the side when relevant | `SentNewMessageEvent`, `SaveSuccessServerEvent`, `SyncEndedOnClientEvent` | `WorldChatService.cs:14`; `README.md:451` |
| Constants | `private const string` for error/template strings at top of class; substitute via Humanizer `FormatWith(...)` | — | `README.md:488-490` |
| i18n keys | `SCREAMING_SNAKE_CASE`, `<CATEGORY>__<LABEL>` with `__` separator | `MAIN_MENU__START_SINGLEPLAYER_BUTTON`, `HUD__TEST_1_BUTTON`, `SETTING_MENU__NICK_HINT` | `Assets/Locales/messages.pot`; `MainPage.cs:49-53` |
| Enums | PascalCase members | `CharacterStat.MaxHp`, `TransferChannel.Chat`, `StatusEffectType.Positive` | `CharacterStat.cs:4-22`; `Consts.cs:15-20` |

### 2) Formatting and Linting

- **Formatter/style:** `.editorconfig` (the only style config) — `*.cs`: UTF-8, LF, **max line length 120** (`.editorconfig:1-5`).
- **Linter:** None beyond C# compiler defaults. `<NoWarn>CS0649</NoWarn>` suppresses the "field never assigned" warning — this is deliberate so DI-injected fields (`[Child]`, `[Logger]`, etc.) don't warn (`NeonWarfare.csproj:14-15`).
- **Most relevant enforced rules:** UTF-8 charset; LF line endings; 120-char max line; DI fields exempt from CS0649.
- **Run commands:** No `dotnet format`/lint script is wired up. Formatting is editor-enforced (Rider/Godot).

### 3) Import and Module Conventions

- **Global usings are centralized** in `Scripts/GlobalUsings.cs` — the only place to add `global using`. It static-imports `Services.Global` (exposing `Di`, `Net` unqualified), `Consts.Global` (`ServerId`, `BroadcastId`), and KludgeBox extension classes (`CameraExtensions`, `ColorExtensions`, `MathExtensions`, `Node2DExtensions`, `NodeTreeExtensions`, `RectExtensions`, `SpriteExtensions`, `VectorExtensions` — hence `Vec2(x,y)` works anywhere) (`GlobalUsings.cs:1-12`; `README.md:311-315`).
- **Per-file using aliasing:** Some files static-import enum namespaces to write attributes unqualified, e.g. `using static Godot.MultiplayerApi.RpcMode;` and `using static Godot.MultiplayerPeer.TransferModeEnum;` so `[Rpc(AnyPeer, ..., TransferMode = Unreliable)]` reads cleanly (`CharacterSynchronizer_Controller.cs:5-6`).
- **DI is field-injection, not constructor injection** for nodes: `[Child]`/`[Parent]`/`[SceneService]`/`[Logger]` populate properties/fields; `Di.Process(this)` is called as the **first line of `_Ready()`** (or constructor for non-nodes) (`README.md:341-343`).

### 4) Error and Logging Conventions

- **Role guards (server/client).** Never call `GetMultiplayer().IsServer()` directly — use `Net.*`. Methods valid on one side begin with a guard:
  ```csharp
  if (!Net.IsServer()) throw new InvalidOperationException("Can only be executed on the server");
  ```
  (`README.md:442-447`; `WorldCommandService.cs:36-42`)
- **Logging — Serilog via `[Logger] private ILogger _log`**, named template parameters:
  ```csharp
  _log.Information("Connecting to the server at {host}:{port}", host, port);
  ```
  `--godot-log-push` duplicates Serilog output to the Godot console (`BaseRootStarter.cs:27`; `README.md:475-479`).
- **Exception strategy:** A global unhandled-exception handler is installed at startup (`Services.ExceptionHandler.AddExceptionHandlerForUnhandledException()`, `BaseRootStarter.cs:28`). Domain errors use typed exceptions (`SaveException`, `LoadException` in `SaveLoadService.cs:14-15`) and `InvalidOperationException` for misuse. RPC receivers frequently guard with early `return` rather than throwing (e.g. `if (!Net.IsClient()) return;` in synchronizer receivers).
- **Sensitive-data redaction:** `[TODO]` — no explicit redaction rules found; the project is a local co-op game with no secrets beyond per-session player data.

### 5) Testing Conventions

- **Test file naming/location:** **No test directory or test project exists.** There is no `tests/`, `*.Tests.csproj`, or `[Test]`/`[Fact]` attribute anywhere in the scan. See TESTING.md.
- **Ad-hoc in-game testing:** Temporary debug lives in the HUD: `Test1`/`Test2`/`Test3` buttons call `World.Test1()/2/3()` RPCs (marked `//TODO Test methods. Remove after tests.`, `World.cs:98`) to spawn bots/enemies; `Log` button dumps the node tree (`Hud.cs:42-45`). These are **production TODOs** (not a test framework).
- **Coverage expectation:** None defined.

### 6) Project-Specific Conventions (C# → Godot)

These are documented in the README and confirmed in code; they are non-obvious and load-bearing:

- **`float` vs `double`:** Vanilla Godot — time in `double`, coordinates/angles in `float` (cast `deltaTime` to `float` for visuals). Critical values (unit stats, damage, healing) use `double` (`README.md:481-485`; `CharacterStats.cs:33,52` use `double`).
- **Client/server checks:** Always `Net.IsServer()`/`IsClient()`, never raw `GetMultiplayer().IsServer()` (`README.md:442`).
- **RPC rules:** Always specify mode explicitly (`[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]` for client→server; `[Rpc(CallLocal = true)]` for server→client). For "chatty" streams set `TransferChannel = (int) Consts.TransferChannel.X`. Arguments are Godot primitives or MessagePack `byte[]` — **no JSON over the network**. Batch data into one call (e.g. multiple unit coords) rather than one RPC per object (`README.md:453-462`; `WorldChatService.cs:28`).
- **Serialization:** Network + saves = **MessagePack** (`[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]`). On-disk settings files = **JSON** (`System.Text.Json`, with a `ColorJsonConverter`) (`README.md:464-467`; `PlayerData.cs:7`; `GameSettingsBase.cs:10-15`).
- **Data classes hold state + sync only** — no game logic. Models are `ObservableObject` + `[ObservableProperty]`; storage subscribes to `PropertyChanged` and broadcasts — **assigning a model property on the server generates network traffic** (`README.md:469-473`; `GeneralDataStorage.cs:33-42`).
- **Initialization before `_Ready()`:** `InitPreReady(...)` returns `this` for data needed before ready (`PackedScene.Instantiate<Hud>().InitPreReady(world)`); `InitPostReady(...)` after. Heavy top-level init is split into `Init()` and `Start()` (`README.md:435-438`; `Hud.cs:28-36`; `BaseRootStarter.cs:21-42`).
- **Memory-leak prevention:** `MultiplayerApi`/service subscriptions are stored in local functions so they can be unsubscribed (`GetMultiplayer().ConnectedToServer -= ConnectedToServerEvent`); such spots carry an explanatory comment because Game→MainMenu transitions are a common leak source (`README.md:494-497`; `Network.cs:28-32`).
- **Adding a chat command:** create a class implementing `ICommandProcessor` in `Scenes/World/Service/Command/Impl/` — it is auto-discovered by reflection (`README.md:381-383`; `WorldCommandService.cs:44-69`).

### 7) Evidence

- `.editorconfig:1-5` — formatting rules.
- `NeonWarfare.csproj:14-15` — `NoWarn CS0649`.
- `Scripts/GlobalUsings.cs:1-12` — global imports policy.
- `Scenes/Entity/Characters/Synchronizer/CharacterSynchronizer_Controller.cs:5-6,69-70` — RPC pattern + enum static imports.
- `Scripts/Service/SaveLoadService.cs:14-15` — typed exceptions.
- `Scenes/World/Data/PersistenceData/Player/PlayerData.cs:7-30` — MessagePack + ObservableProperty model.
- `README.md:429-497` — authored conventions (cross-checked against code).
