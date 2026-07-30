# Testing

[← Project README](../README.md)

The tests live in a separate project, `Tests/NeonWarfare.Tests`; the framework is **xUnit v3**.
Running them:

```bash
dotnet test                                              # all tests
dotnet test --filter FullyQualifiedName~DocsLinksTests   # a single class
```

In Rider the tests are visible as usual: the test project is part of `NeonWarfare.sln`.

## Principles

* **Unit tests only.** Godot is not launched from the tests: neither the engine, nor the scenes, nor
  the nodes. That is why the test project is built with the plain `Microsoft.NET.Sdk` and does
  **not** reference `NeonWarfare.csproj` — the reference would pull in `GodotSharp`, which does not
  initialize outside a Godot process.
* It follows that exactly two things can be tested: **pure logic** (classes that do not depend on the
  engine) and **repository invariants** (file structure, documentation, locales, conventions).
  Everything that lives inside the node tree is still verified by the build and by a manual run of
  the game — see [Quick start](Quick-start.md).
* **A test collects all the violations and prints them as a single list** instead of failing on the
  first one. Otherwise the documentation has to be fixed in iterations. `Infrastructure/FailureReport`
  exists for this.
* A check that goes file by file is written as a `[Theory]` with one file per case — then the report
  shows exactly which file is broken.

## Conventions

* Location: `Tests/NeonWarfare.Tests/<Area>/<Subject>Tests.cs`, reusable helpers — in
  `Infrastructure/`.
* The namespace mirrors the path, just as in the game project (see
  [Code style conventions](Code-style.md)).
* The method name is `<What>_<Expectation>`: `Links_PointToExistingFiles`, `File_UsesLineFeedOnly`.
* The path to the repository root is taken from `RepositoryPaths` rather than assembled by hand: it is
  baked into the assembly through `AssemblyMetadata` in the `.csproj`, because a test's working
  directory is `bin/<config>/<tfm>/`.

## What is covered now

Only the documentation — the tests in `Tests/NeonWarfare.Tests/Docs/`. They check that the links and
anchors inside `Docs/**/*.md` and `README.md` have not gone stale, that every `Docs/` file is linked
from `README.md` and contains a uniform back-anchor to it, and that the file format matches
`.editorconfig`. `CLAUDE.md` is not scanned: it deliberately has no markdown links.

## What is planned to be covered

None of the following is implemented yet — this is a roadmap, not a description of what exists.

**Localization.** All the keys from `Services.I18N.Tr(KEY)` are present in all three
`Assets/Locales/*.po` files; there are no orphaned keys that are absent from the code; `messages.pot`
has not fallen behind the code. All three files in `Assets/Locales/` have the same set of keys.
The rules are in [Localization](Localization.md).

**Code conventions that can be checked statically.** The namespace matches the file path; next to
every `.tscn` there is a `.cs` with the same name; every public RPC wrapper has a private `*Rpc`
counterpart; the code contains no `GetMultiplayer().IsServer()` instead of the `Net.*` helpers.
These are exactly the mistakes the compiler does not catch — see
[Code style conventions](Code-style.md).

**Pure logic without Godot.** Argument parsing from `Scripts/Content/CmdArgs/`
([Command-line arguments](Cli-args.md)), the `ICommandProcessor` chat commands
([Chat and commands](Arch/Chat-and-commands.md)), the math of stats and status effects
([Entities](Arch/Entities.md)) — as the logic gets moved out of the nodes into testable classes.

**Environment synchronization.** `Properties/launchSettings.json` and `.run/*.run.xml` match the
tables in [Quick start](Quick-start.md) — that invariant is written down there, but nobody checks it.

**CI.** A `dotnet test` run on every push. Right now there is no CI in the repository at all.

## What will not be here

Integration tests that bring up Godot (a headless launch, scene tree checks, server + client network
tests). They require a different tool — the engine has to run the tests itself, from inside the
process. If such tests are needed, that is a separate decision (gdUnit4 or an equivalent) and a
separate project, not an extension of `NeonWarfare.Tests`. Network scenarios are still verified
manually: a server plus at least one client, see [Quick start](Quick-start.md).
