# Stack and dependencies

[← Project README](../README.md)

**Godot:** the latest version, `Forward+` renderer. **.NET:** the latest version.

Packages of the game project (`NeonWarfare.csproj`):

| Package | What for |
|---|---|
| `KludgeBox` | An in-house library with shared reusable code: DI, logging, services, Godot extensions, utility classes |
| `RepliCAT` | An in-house library for delta replication of plain C# objects |
| `MessagePack` | Binary serialization of the world state for saves and for transfer over the network |
| `Microsoft.Extensions.DependencyInjection` | The container of world services, built by `World` from the layer attributes |
| `JetBrains.Annotations` | `[MeansImplicitUse]`, so the IDE does not flag reflection-called methods (`[EventHandler]`) as unused |

Packages of the test project (`Tests/NeonWarfare.RepoTests/NeonWarfare.RepoTests.csproj`), more detail — in
[Testing](Testing.md):

| Package | What for |
|---|---|
| `xunit.v3` | The test framework: `[Fact]`, `[Theory]`, `Assert` |
| `xunit.runner.visualstudio` | The VSTest adapter — without it `dotnet test` and Rider do not find the tests |
| `Microsoft.NET.Test.Sdk` | The VSTest host, enables the `dotnet test` target |
| `Microsoft.CodeAnalysis.CSharp` | The C# parser (Roslyn), convention tests check a syntax tree |
| `Mono.Cecil` | Reads the compiled game assembly as metadata and IL for the World layer rules, never loads it |
| `GodotSharp` | Compile-time only, no runtime asset: a reference for the `GodotBoxIndependenceTests` compilation |
| `KludgeBox` | Compile-time only, never loaded: the other reference for the `GodotBoxIndependenceTests` compilation |

The smoke test project (`Tests/NeonWarfare.SmokeTests/NeonWarfare.SmokeTests.csproj`) takes the same
three xUnit packages and nothing else — it launches the game as an external process and needs no parser,
see [Smoke testing](Smoke-testing.md).

Packages of the game test project (`Tests/NeonWarfare.GameTests/NeonWarfare.GameTests.csproj`), a
`Godot.NET.Sdk` project that references the game, more detail — in [Game testing](Game-testing.md):

| Package | What for |
|---|---|
| `gdUnit4.api` | The test framework: `[TestSuite]`, `[TestCase]`, `[RequireGodotRuntime]`, `AssertThat`; a release candidate, see the document |
| `gdUnit4.test.adapter` | The VSTest adapter — discovers the tests and starts Godot for them |
| `Microsoft.NET.Test.Sdk` | The VSTest host, enables the `dotnet test` target |

Coming in transitively through KludgeBox and used directly in the code: **Serilog** — logging
(`LogFactory.GetForStatic<T>()`, see [Code style conventions](Code-style.md#logging)); **Humanizer** —
substitution into string templates (`FormatWith(...)`).

**KludgeBox and GodotBox.** KludgeBox is referenced as a NuGet package, so its sources are not in this
repository and searching it will not find declarations of its types: the DI core (`DependencyInjector`,
`[Child]`, `[NotNull]`), logging, the services of the [global registry](Services.md) (`I18N`,
`Rand`, `NodeTree`, …), the Godot extensions pulled in by `GlobalUsings.cs` and so on. The path to the
library's source code is stored in the `KLUDGEBOX_SRC` ENV variable — that is where they should be read.

The Godot nodes built on top of it are **not** in the package: they live in this repository, in
`Src/GodotBox/` (namespaces `GodotBox.*`) — `NodeContainer`, `AbstractStorage`, `CheckedAbstractStorage`,
`Background`, `Camera` with its shifts, `ProcessShutdowner`, `ProcessDeadChecker`, plus its own service
registry `GodotBoxServices`. GodotBox is a reusable layer that must not depend on the game — see
[Code style conventions](Code-style.md#namespaces).

One build detail: `NeonWarfare.csproj` turns the default `Compile` glob off (`EnableDefaultCompileItems` set
to `false`) and lists its sources itself: `<Compile Include="Src/**/*.cs" />`. The game project's directory is
the repository root, so the default glob (`**/*.cs`) would otherwise pull the test files into the game
assembly, and it would fail on the xUnit types; all the game code lives in `Src/`. `Godot.NET.Sdk` already
turns the default `None` glob off, so the `None` items for `README.md`, `Docs/`, `Assets/` and
`launchSettings.json` are listed explicitly. The test projects build on their own; in `ExportDebug` and
`ExportRelease` they are excluded from the solution build so that the Godot editor and the game export do not
touch them.
