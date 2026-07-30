# Stack and dependencies

[← Project README](../README.md)

* **Godot:** the latest version, `Forward+` renderer.
* **.NET:** the latest version.

Packages of the game project (`NeonWarfare.csproj`):

| Package | What for |
|---|---|
| `KludgeBox` | An in-house library with shared reusable code: DI, logging, utility Godot nodes, utility classes |
| `CommunityToolkit.Mvvm` | The `[ObservableProperty]` annotation for data models |
| `MessagePack` | Binary serialization of the world state for saves and for transfer over the network |

Packages of the test project (`Tests/NeonWarfare.Tests/NeonWarfare.Tests.csproj`), more detail — in
[Testing](Testing.md):

| Package | What for |
|---|---|
| `xunit.v3` | The test framework: `[Fact]`, `[Theory]`, `Assert` |
| `xunit.runner.visualstudio` | The VSTest adapter — without it `dotnet test` and Rider do not find the tests |
| `Microsoft.NET.Test.Sdk` | The VSTest host, enables the `dotnet test` target |

**About KludgeBox.** The library's sources are not in this repository — it is referenced as a NuGet
package, so searching the repository will not find declarations of its types (`NodeContainer`,
`AbstractMultiplayerSpawner`, `ProcessShutdowner`, `ProcessDeadChecker`,
`StatModifiersContainer<T>`, the `[Sync]` attribute and so on). The path to the library's source code
is stored in the `KLUDGEBOX_SRC` ENV variable — that is where they should be read.

Coming in transitively through KludgeBox and used directly in the code:

* **Serilog** — logging (`[Logger] private ILogger _log`);
* **Humanizer** — substitution into string templates (`FormatWith(...)`).

The `CS0649` warning is suppressed for the build (`NoWarn` in `.csproj`): the fields are filled by DI
rather than by a constructor, and the compiler considers them unused.

`NeonWarfare.csproj` contains `<Compile Remove="Tests/**" />`: the game project's directory is the
repository root, so otherwise the default `Godot.NET.Sdk` glob (`**/*.cs`) would pull the test files
into the game assembly, and it would fail on the xUnit types. The test project builds on its own; in
`ExportDebug` and `ExportRelease` it is excluded from the solution build so that the Godot editor and
the game export do not touch it.
