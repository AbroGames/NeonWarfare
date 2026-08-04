# Testing

[← Project README](../README.md)

Tests live in `Tests/NeonWarfare.Tests`, framework — **xUnit v3**. The project is part of
`NeonWarfare.sln`, so Rider sees the tests as usual.

```bash
dotnet test                                              # all tests
dotnet test --filter FullyQualifiedName~DocsLinksTests   # a single class
```

## Principles

* **Unit tests only.** Godot is never launched. The project is built with the plain
  `Microsoft.NET.Sdk` and does **not** reference `NeonWarfare.csproj`: that would pull in `GodotSharp`,
  which does not initialize outside a Godot process.
* Hence only two things are testable: **pure logic** and **repository invariants** (files,
  documentation, locales, conventions). Everything inside the node tree is covered by the build and a
  manual run — see [Quick start](Quick-start.md).
* **A test collects all violations into one list** instead of failing on the first —
  `Infrastructure/FailureReport`. A per-file check is a `[Theory]` with one file per case.
* **Conventions are checked on a syntax tree, not on text** (`Microsoft.CodeAnalysis.CSharp` — a parser
  only, no `GodotSharp`). Text matching cannot tell a declaration from a call, and
  [Code style conventions](Code-style.md) is written in those terms.

## Conventions

* Location: `Tests/NeonWarfare.Tests/<Area>/<Subject>Tests.cs`, shared helpers — `Infrastructure/`.
  The namespace mirrors the path, as in the game project.
* Method name: `<What>_<Expectation>` — `Links_PointToExistingFiles`, `File_UsesLineFeedOnly`.
* The repository root comes from `RepositoryPaths` (baked in via `AssemblyMetadata`; the working
  directory is `bin/<config>/<tfm>/`), never assembled by hand.
* Read a file through the helper for its format, not `File.ReadAllText`: `MarkdownDocument`,
  `CSharpFile`, `PoFile`, `SceneFile` (`.tscn` / `.tres`), `GodotProjectFile`. `PoFile` and `SceneFile`
  throw on anything unexpected instead of skipping it.
* `UidIndex` maps every `uid://` to its file: a `.uid` sidecar for code and shaders, the header of a
  `.tscn` / `.tres`, or the `.import` of a converted asset.
* An exception to a rule is an explicit array in the test with a comment saying why — never a silent
  skip. Four exist: `Scripts/GlobalUsings.cs` (no namespace), `RootStarterManager` (reads the command
  line directly), the engine's `ui_*` input actions, `NavigationService` (not a world service).

## What is covered now

One row per test class, path relative to `Tests/NeonWarfare.Tests/`. A new test = a new row.

| Test class | What it checks |
| --- | --- |
| `Docs/DocsLinksTests` | Links and anchors in `Docs/**/*.md` and `README.md` resolve |
| `Docs/DocsReadmeIndexTests` | Every `Docs/` file is linked from `README.md` |
| `Docs/DocsBackLinkTests` | Every `Docs/` file starts with a heading and the back-anchor to `README.md` |
| `Docs/DocsFormattingTests` | Encoding, line endings, trailing whitespace, line length, one heading, final newline |
| `Docs/CliArgsDocTests` | Flag table of [Command-line arguments](Cli-args.md) ↔ `Scripts/Content/CmdArgs/` |
| `Docs/StackDocTests` | Package tables of [Stack](Stack.md) ↔ `PackageReference` of both `.csproj` |
| `Docs/ServicesDocTests` | Both tables of [Services](Services.md) ↔ `Scripts/Services.cs` and `World*Service` |
| `Docs/TestingDocTests` | This table ↔ the test classes of `Tests/NeonWarfare.Tests/`, both ways |
| `Docs/RepositoryStructureDocTests` | Paths drawn in [Repository structure](Repository-structure.md) exist (one way only) |
| `Localization/LocaleFilesTests` | One key set, key order, no duplicates, naming, no empty `.po` translations, empty `.pot` |
| `Localization/LocalizationUsageTests` | Keys ↔ usages in `.cs` and `.tscn`, both ways |
| `Conventions/NamespaceTests` | Namespace matches the file path |
| `Conventions/RpcConventionTests` | RPC targets are private, carry an explicit `[Rpc(...)]`, called once from their wrapper |
| `Conventions/RoleCheckTests` | Process role read from `Net.*`, not `GetMultiplayer()` or a peer-id literal |
| `Conventions/CmdArgsContractTests` | Flags, parsing and `CmdArgsService` stay where [Cli args](Cli-args.md) says |
| `Conventions/DiTests` | `Di.Process(this)` is the first statement; every class with injected members calls it |
| `Conventions/ChildInjectionTests` | A `[Child]` member name resolves to a reachable node in the scene |
| `Conventions/InputActionTests` | `Keys.cs` ↔ the `[input]` section of `project.godot`, both ways |
| `Conventions/CodeStyleTests` | `Event` suffix on events, single `GlobalUsings.cs`, no `GD.Load` / `res://` literals |
| `Conventions/FileEncodingTests` | Every text file of the repository: LF line endings; the BOM check is written but off |
| `Launch/LaunchProfilesTests` | `launchSettings.json` ↔ [Quick start](Quick-start.md): profiles, arguments, order, `--path` |
| `Launch/MultiLaunchTests` | `.run/` configs ↔ the document and ↔ existing profiles; file name matches config name |
| `Scenes/SceneResourceTests` | Every `res://` path resolves; a root node's script is the `.cs` beside the scene |
| `Scenes/UidReferenceTests` | `ext_resource` uids resolve, agree with `path=`, are unique; `project.godot` uids resolve |
| `Scenes/SidecarFileTests` | Every `.cs` has its `.cs.uid`; no `.uid` or `.import` outlived its file |

Godot loads by `uid://` and treats `path=` as a hint, so a stale path survives a rename unnoticed —
hence both sides are checked. `CLAUDE.md` is not scanned: it has no markdown links by design. The
structure tree is checked one way deliberately; the reverse would be a different document.

## CI

`.github/workflows/build.yml` runs `dotnet restore`, `build`, `test` on every push into `master` and
every pull request into it. Godot is not installed on the runner — `Godot.NET.Sdk` and `GodotSharp` come
from NuGet, and the tests never start the engine.

## What will not be here

Integration tests that bring up Godot (headless launch, scene tree, server-plus-client networking). They
need the engine to run the tests from inside its own process — a separate decision (gdUnit4 or an
equivalent) and a separate project, not an extension of `NeonWarfare.Tests`. Network scenarios stay
manual: a server plus at least one client, see [Quick start](Quick-start.md).
