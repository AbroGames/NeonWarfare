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
* **The conventions are checked on a syntax tree, not on text.** `Microsoft.CodeAnalysis.CSharp` is
  the C# parser on its own — it neither compiles nor runs the game, so it brings no `GodotSharp` with
  it and the rule above still holds. Text matching cannot tell a declaration from a call or code from
  a comment, and the rules in [Code style conventions](Code-style.md) are written in exactly those
  terms.

## Conventions

* Location: `Tests/NeonWarfare.Tests/<Area>/<Subject>Tests.cs`, reusable helpers — in
  `Infrastructure/`.
* The namespace mirrors the path, just as in the game project (see
  [Code style conventions](Code-style.md)).
* The method name is `<What>_<Expectation>`: `Links_PointToExistingFiles`, `File_UsesLineFeedOnly`.
* The path to the repository root is taken from `RepositoryPaths` rather than assembled by hand: it is
  baked into the assembly through `AssemblyMetadata` in the `.csproj`, because a test's working
  directory is `bin/<config>/<tfm>/`.
* A file is read through the helper for its format, not with `File.ReadAllText`: `MarkdownDocument`
  for `Docs/`, `CSharpFile` for the game sources, `PoFile` for `Assets/Locales/`, `SceneFile` for
  `.tscn` and `.tres`, `GodotProjectFile` for `project.godot`. `PoFile` and `SceneFile` throw on
  anything they were not written for instead of skipping it, so a test can never quietly pass on a file
  it misread.
* `UidIndex` maps every `uid://` in the repository to its file. A uid lives in one of three places
  depending on the kind of file: a `.uid` sidecar for code and shaders, the header of a `.tscn` /
  `.tres`, the `.import` settings of an asset Godot converts on load.
* An exception to a rule is an explicit array inside the test, with a comment saying why — never a
  silently skipped case. There are four: `Scripts/GlobalUsings.cs` declares no namespace,
  `RootStarterManager` reads the command line without `CmdArgsService`, the `ui_*` input actions come
  from the engine instead of `project.godot`, and `NavigationService` lives among the world services
  without being one.

## What is covered now

**Documentation** — `Docs/`. Links and anchors inside `Docs/**/*.md` and `README.md` have not gone
stale, every `Docs/` file is linked from `README.md` and contains a uniform back-anchor to it, the file
format matches `.editorconfig`, and the flag table in [Command-line arguments](Cli-args.md) agrees
with the flags declared in `Scripts/Content/CmdArgs/`. `CLAUDE.md` is not scanned: it deliberately has
no markdown links.

The documents that describe the code are checked against it in both directions: the package tables in
[Stack](Stack.md) against the `PackageReference` items of the two `.csproj`, both tables in
[Services](Services.md) against `Scripts/Services.cs` and the `World*Service` classes, and every path
drawn in the tree of [Repository structure](Repository-structure.md) against the file system. Only that
one direction for the tree — it is deliberately partial, and demanding the reverse would be demanding a
different document.

**Localization** — `Localization/`. All three files in `Assets/Locales/` carry the same set of keys,
the keys follow the `SCREAMING_SNAKE_CASE` naming with `__` from [Localization](Localization.md), the
`.po` files have no empty translations and `messages.pot` has nothing but empty ones. Keys and code are
checked in both directions: every key used in a `.cs` or a `.tscn` exists in the locale files, and every
key in the locale files is used somewhere. Any key-shaped literal counts as a usage, not only the
argument of `Tr(...)` — keys also live in `[Name(...)]` / `[Hint(...)]`, in `LoadingScreenTypes` and in
the scenes themselves.

**Code conventions** — `Conventions/`. The namespace matches the file path. RPC targets are private,
carry an `[Rpc(...)]` with the mode spelled out, and are reached exactly once — from their own wrapper.
The process role is asked from `Net.*`, never from `GetMultiplayer()` and never as a peer id compared
with a number. The command-line contract from [Command-line arguments](Cli-args.md) holds: flags are
declared and parsed only in `Scripts/Content/CmdArgs/`, `CmdArgsService` is named only in the Root
starters, and nobody reads `OS.GetCmdlineArgs()` on their own.

The injection contract from [Dependency injection](Dependency-injection.md) holds as well:
`Di.Process(this)` is the first statement of the method it is called in, every class with a `[Child]` /
`[Parent]` / `[SceneService]` / `[Logger]` member runs it — itself or through a base class — and the
name of a `[Child]` member answers to a node reachable from the one its script is attached to. The
smaller rules: an `event` is named with the `Event` suffix, `global using` lives only in
`Scripts/GlobalUsings.cs`, nothing reaches a resource through `GD.Load` or a `res://` literal instead of
a `CheckedAbstractStorage`, and the input actions named in `Keys.cs` match the `[input]` section of
`project.godot` in both directions.

**Launch profiles** — `Launch/`. The profiles of `Properties/launchSettings.json` and the Multi-Launch
configurations of `.run/` agree with [Quick start](Quick-start.md) in both directions and in the same
order: every profile has a table row carrying the arguments it really passes, every `.run/` file has a
list item naming the tasks it really starts, and every task refers to a profile that exists. The last
one is the reason for the rest: a task names its profile by a plain string, so renaming the profile
leaves a configuration that Rider rejects only at the moment someone presses Run.

**Scenes** — `Scenes/`. Every `res://` path in a `.tscn` or a `.tres` resolves to a file that exists,
and the script of a root node is the `.cs` next to the scene under the same name. Godot resolves
references by `uid://` and treats `path=` as a hint, so a stale path survives a rename unnoticed —
which is how two of them had been living in the repository.

The uid side is checked too, and it is the side the engine actually loads by: every `uid://` in an
`ext_resource` resolves to a file, the uid and the `path=` written next to it name the same one, no two
files claim the same uid, and the uid settings of `project.godot` — the main scene, the icon, the theme,
which have no path next to them at all — resolve as well. Plus the sidecars themselves: every `.cs` has
its `.cs.uid`, and no `.uid` or `.import` outlived the file it belongs to.

## CI

`.github/workflows/build.yml` runs on every push into `master` and on every pull request into it:
`dotnet restore`, `dotnet build`, `dotnet test`. Godot is not installed on the runner — `Godot.NET.Sdk`
and `GodotSharp` come from NuGet, and the tests never start the engine. The workflow is a copy of the
one in KludgeBox, with the difference that here the `Test` step actually runs something.

## What will not be here

Integration tests that bring up Godot (a headless launch, scene tree checks, server plus client network
tests). They require a different tool — the engine has to run the tests itself, from inside the
process. If such tests are needed, that is a separate decision (gdUnit4 or an equivalent) and a
separate project, not an extension of `NeonWarfare.Tests`. Network scenarios are still verified
manually: a server plus at least one client, see [Quick start](Quick-start.md).
