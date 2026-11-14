# Testing

[← Project README](../README.md)

Tests live in `Tests/NeonWarfare.RepoTests`, framework — **xUnit v3**. The project is part of
`NeonWarfare.sln`, so Rider sees the tests as usual.

```bash
dotnet test                                              # all tests
dotnet test --filter FullyQualifiedName~DocsLinksTests   # a single class
```

## Principles

* **Unit tests only.** Godot is never launched: the project is built with the plain
  `Microsoft.NET.Sdk` and references `NeonWarfare.csproj` for build order only
  (`ReferenceOutputAssembly="false"`) — its types would pull in `GodotSharp`, which does not initialize
  outside a Godot process. `Architecture/` reads the built game assembly as metadata (Mono.Cecil).
* Hence only two things are testable here: **pure logic** and **repository invariants** (files,
  documentation, locales, conventions). What lives inside the node tree is covered by the build, by the
  [game tests](Game-testing.md), by the [smoke tests](Smoke-testing.md) and by a manual run — see
  [Quick start](Quick-start.md).
* **A test collects all violations into one list** instead of failing on the first —
  `Infrastructure/FailureReport`. A per-file check is a `[Theory]` with one file per case.
* **Conventions are checked on a syntax tree, not on text** (`Microsoft.CodeAnalysis.CSharp`): text
  matching cannot tell a declaration from a call, and [Code style conventions](Code-style.md) is written
  in those terms.

## Conventions

* Location: `Tests/NeonWarfare.RepoTests/<Area>/<Subject>Tests.cs`, shared helpers — `Infrastructure/`; the
  namespace mirrors the path, as in the game project. Method name: `<What>_<Expectation>`
  (`Links_PointToExistingFiles`, `File_UsesLineFeedOnly`).
* The repository root comes from `RepositoryPaths` (baked in via `AssemblyMetadata`; the working
  directory is `bin/<config>/<tfm>/`), never assembled by hand. Its whole-repository lists
  (`AllFiles`, `TextFiles`) come from `git ls-files --cached --others --exclude-standard`: whatever
  `.gitignore` or `.git/info/exclude` hides (`.claude/worktrees/`, `.claude/tasks/`) is not scanned, so
  the tests need a git checkout and `git` on `PATH`.
* Read a file through the helper for its format, not `File.ReadAllText`: `MarkdownDocument`,
  `CSharpFile`, `PoFile`, `SceneFile` (`.tscn` / `.tres`), `GodotProjectFile`, `EditorConfigFile`; underneath them all
  `TextFile` is the one place that turns bytes into lines. `PoFile` and `SceneFile` throw on anything
  unexpected instead of skipping it. `UidIndex` maps every `uid://` to its file: a `.uid` sidecar for
  code and shaders, the header of a `.tscn` / `.tres`, or the `.import` of a converted asset.
* A document is read section by section: `MarkdownDocument.Section(heading)` throws when the heading is
  gone, and `RequireTable(...)` names the columns before anything reads a cell by index. The repeated
  shapes live in `Infrastructure/`: `CrossCheck` walks a "code ↔ document" pair both ways,
  `DocTableChecks` states what an inventory row must look like, `FileSources` holds the theory sources.
* An exception to a rule is an explicit array in the test with a comment saying why — never a silent
  skip. Nine exist: `GlobalUsings.cs` (no namespace), `RootStarterManager` (reads the command
  line directly), the engine's `ui_*` input actions, `NavigationService` (not a world service),
  `NotFoundCommand` (no command name of its own), Godot's `--path` (not our flag), the composition
  root — `World` and `WorldServicesBuilder` as one entry (takes `Services` and wires every layer),
  `Services.Di`, the `All tests` launch profile (does not start the game). Each has a test
  failing with "stale exception" once the entry names nothing (`CrossCheck.AssertExemptionsExist`).

## What is covered now

One row per test class, path relative to `Tests/NeonWarfare.RepoTests/`. A new test = a new row.

| Test class | What it checks |
| --- | --- |
| `Docs/DocsLinksTests` | Links and anchors in `Docs/**/*.md` and `README.md` resolve |
| `Docs/DocsReadmeIndexTests` | Every `Docs/` file is linked from `README.md` |
| `Docs/DocsBackLinkTests` | Every `Docs/` file starts with a heading and the back-anchor to `README.md` |
| `Docs/DocsFormattingTests` | Encoding, trailing whitespace, line length, one heading, final newline |
| `Docs/CliArgsDocTests` | Flag table of [Command-line arguments](Cli-args.md) ↔ `Scripts/Content/CmdArgs/` |
| `Docs/StackDocTests` | Package tables of [Stack](Stack.md) ↔ `PackageReference` of both `.csproj`; the smoke project takes only the xUnit three |
| `Docs/ServicesDocTests` | Both tables of [Services](Services.md) ↔ `Scripts/Services.cs` and `World*Service` |
| `Docs/TestingDocTests` | This table ↔ the test classes of `Tests/NeonWarfare.RepoTests/`, both ways |
| `Docs/GameTestingDocTests` | Coverage table of [Game testing](Game-testing.md) ↔ the `[TestSuite]` classes of `Tests/NeonWarfare.GameTests/`, both ways |
| `Docs/SmokeTestingDocTests` | Scenario table of [Smoke testing](Smoke-testing.md) ↔ the tests of `Tests/NeonWarfare.SmokeTests/` |
| `Docs/ChatCommandsDocTests` | Command table of [Chat and commands](Chat-and-commands.md) ↔ the `ICommandProcessor` classes: name, rights, only in `Command/Impl/` |
| `Docs/NetworkingDocTests` | Channel table of [Networking](Networking.md) ↔ `Consts.TransferChannel`, order included |
| `Docs/RepositoryStructureDocTests` | Paths drawn in [Repository structure](Repository-structure.md) exist (one way only) |
| `Localization/LocaleFilesTests` | One key set, key order, no duplicates, naming, no empty `.po` translations, empty `.pot` |
| `Localization/LocalizationUsageTests` | Keys ↔ usages in `.cs` and `.tscn`, both ways |
| `Conventions/NamespaceTests` | Namespace matches the folder path relative to `Src/` |
| `Conventions/GodotBoxIndependenceTests` | `Src/GodotBox` compiles without the game |
| `Conventions/RpcConventionTests` | RPC targets are private, carry an explicit `[Rpc(...)]`, called once from their wrapper |
| `Conventions/RoleCheckTests` | Process role read from `Net.*`, not `GetMultiplayer()` or a peer-id literal |
| `Conventions/CmdArgsContractTests` | Flags, parsing and `CmdArgsService` stay where [Cli args](Cli-args.md) says |
| `Conventions/DiTests` | `Di.Process(this)` is the first statement; every class with injected members calls it; an override of the method running it calls `base` |
| `Conventions/ChildInjectionTests` | A `[Child]` member name resolves to a reachable node in the scene |
| `Conventions/InputActionTests` | `Keys.cs` ↔ the `[input]` section of `project.godot`, both ways |
| `Conventions/CodeStyleTests` | `Event` suffix on events, single `GlobalUsings.cs`, no `GD.Load` / `res://` literals |
| `Conventions/SourceFormattingTests` | Every hand-written `.cs`, tests included: lines fit into `max_line_length` columns, tabs expanded to `tab_width` |
| `Conventions/FileEncodingTests` | Every text file of the repository, documentation included: LF line endings, no UTF-8 BOM |
| `Architecture/ConstructorLayerTests` | World service constructors take only the layers the layer table allows; `IClientsConnection` only in `ServerNetwork`, `IServerConnection` only in `ClientNetwork`, `EntityRegistry` and `WorldRoot` only in `Simulation`; every `Build` parameter is open or restricted explicitly |
| `Architecture/ConstructorWorldReadTests` | No world service constructor calls a query or an `EntityRegistry` lookup |
| `Architecture/NotSavedEntityTests` | Every replicated member of a `[NotSaved]` entity is a readonly field set in each constructor |
| `Architecture/CommandHandlerTests` | Every command but the join has a player handler (the join goes to `IJoinRequestHandler`); exactly one join and one peer disconnected handler; every handler is `[CommandHandler]` |
| `Architecture/ChatCommandTests` | Every `IChatCommand` is `[SimulationFacade]`, or the root never registers it |
| `Architecture/EventHandlerTests` | An `[EventHandler]` is a private instance `Handle` of one event type, declared in a `[Presentation]` |
| `Architecture/LayerReferenceTests` | `Services` reached in the World only by `World` (`Di` aside); Simulation referred to only by its group; `HudMailbox.Post` called only from the Presentation |
| `Architecture/ModelRulesTests` | Models refer only to primitives, engine value types, RepliCAT, enums, models; only Simulation writes them |
| `Architecture/ReplicatedMemberTests` | `[Replicated]` only on fields, never on a plain collection: RepliCAT ones only |
| `Architecture/SimulationTimingTests` | The Simulation group defers nothing past the tick: no deferred calls, timers, tweens, `async` |
| `Architecture/TickPriorityTests` | `int.MaxValue` physics priority only on `ServerTickNode`, in code and in no scene |
| `Launch/LaunchProfilesTests` | Game profiles of `launchSettings.json` ↔ [Quick start](Quick-start.md): profiles, arguments, order, `--path` |
| `Launch/MultiLaunchTests` | `.run/` configs ↔ the document and ↔ existing profiles; file name matches config name |
| `Scenes/SceneResourceTests` | `res://` paths in scenes and `project.godot` resolve; a root script is the `.cs` beside its scene |
| `Scenes/UidReferenceTests` | `ext_resource` uids resolve, agree with `path=`, are unique; `project.godot` uids resolve |
| `Scenes/SidecarFileTests` | Every `.cs` and `.gdshader` has its `.uid`, every imported asset its `.import`; no sidecar outlived its file |
| `Repository/GodotVersionTests` | `Godot.NET.Sdk` version of the game tests' `.csproj` equals the game's |
| `Repository/RepositoryStatsTests` | Nothing — prints the size of the repository |

Godot loads by `uid://` and treats `path=` as a hint, so a stale path survives a rename unnoticed —
hence both sides are checked. `CLAUDE.md` is not scanned: it has no markdown links by design. The
structure tree is checked one way deliberately; the reverse would be a different document.

## CI and repository size

`.github/workflows/build.yml` runs `dotnet restore`, `build`, `test` on every push into `master` and
every pull request into it. This project needs no engine — `Godot.NET.Sdk` and `GodotSharp` come from
NuGet, and the tests never start it. The runner does download Godot, but only for the
[game tests](Game-testing.md), which run in a later step of their own. Each `test` step names its project
explicitly, so the [smoke tests](Smoke-testing.md) are compiled but not run.

`RepositoryStatsTests` counts and asserts nothing; a verbose runner is what makes its output visible:

```bash
dotnet test --filter FullyQualifiedName~RepositoryStatsTests -l "console;verbosity=detailed"
```

## What will not be here

Tests that run from **inside** the engine's own process and reach into the node tree. They live in a
separate project on a separate framework — [Game testing](Game-testing.md).

Launching the game as an external process and reading its output is a different thing and does exist —
[Smoke testing](Smoke-testing.md), a separate project that covers startup in several modes, including a
server with two clients. Anything beyond starting up stays manual, see [Quick start](Quick-start.md).
