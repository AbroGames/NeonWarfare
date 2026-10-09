# Game testing

[← Project README](../README.md)

`Tests/NeonWarfare.GameTests` tests game code that lives in `Node` descendants, and it does so inside a real
Godot process. The [unit tests](Testing.md) cannot: a `Node` cannot even be constructed outside the
engine — its constructor calls into native Godot code, and so does the static state of every engine
class. The [smoke tests](Smoke-testing.md) run the whole game and only look at its output. Here a test
creates the nodes it needs, calls their methods and checks the result.

Framework — **gdUnit4Net** (`gdUnit4.api` plus the `gdUnit4.test.adapter` VSTest adapter), not xUnit:
xUnit has no way to run a test inside the engine.

## Running

```bash
dotnet test Tests/NeonWarfare.GameTests/NeonWarfare.GameTests.csproj
```

Needs `GODOT_BIN` — the path to a .NET build of Godot, the same file as `GODOT_EXE` (see
[Quick start](Quick-start.md)); the adapter reads no other name. Without it nothing runs and the run fails:
`GameTests.runsettings` turns "no tests executed" into an error. Rider picks the settings file up through
`RunSettingsFilePath` in the `.csproj`, the same as `dotnet test`.

Every run starts Godot headless, which adds a few seconds to the run; the first run in a fresh checkout
takes longer, see below.

## How it works

* The folder is a Godot project of its own: `project.godot` next to a `Godot.NET.Sdk` `.csproj` whose
  assembly is the project's main assembly. The game comes in through a `ProjectReference` to
  `NeonWarfare.csproj`. The game's Godot project does not see this one — `Tests/` holds a `.gdignore`.
* The adapter looks for the Godot project in the nearest folder above the test assembly that holds a
  `.csproj` — `Godot.NET.Sdk` builds into `.godot/mono/temp/bin/`, so that is this folder.
* On the first run the adapter writes its runner into `gdunit4_testadapter_v5/` and rebuilds the project
  with it. It then opens the project in the headless editor once, and that editor writes a `.uid` next to
  every script. Both are generated rather than written by hand, so the folder's own `.gitignore` keeps
  them out of the repository.
* The tests themselves run in `godot --path . -s <runner> --headless`: no scene, no autoloads, nothing
  of the game is started. What a test needs, it builds itself.

## Writing a test

Location and naming follow [Testing](Testing.md#conventions): `<Area>/<Subject>Tests.cs`, the namespace
mirrors the path, method `<What>_<Expectation>`.

```csharp
[TestSuite]
public class NodeContainerTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void ChangeStoredNode_StoresTheNodeAsItsChild()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        ...
        AssertThat(container.GetCurrentStoredNode<Node>()).IsSame(stored);
    }
}
```

* `[RequireGodotRuntime]` is what sends a test into the engine; without it the adapter runs it in plain
  .NET, where creating a node fails.
* A node that never enters a scene tree is freed by nobody: wrap it in `AutoFree(...)`. Its children are
  freed with it.
* `AssertThat` comes from `using static GdUnit4.Assertions;`.
* Lifecycle methods are called directly (`node._Ready()`), with no scene tree; a `QueueFree` is checked
  with `IsQueuedForDeletion()`, which needs no frame.
* A node subclass a test needs goes into `<Area>/Fixtures/`, one per file: Godot binds a script class to
  the file of the same name.

## What is covered now

One row per `[TestSuite]` class, path relative to `Tests/NeonWarfare.GameTests/`

| Test class | What it checks |
| --- | --- |
| `GodotBox/NodeContainerTests` | Storing, replacing and clearing the child; the replaced one is queued for deletion; `_Ready` adopts one child, throws on two |
| `GodotBox/AbstractStorageTests` | Only exported `PackedScene` properties are registered, after `_PreReady` |
| `GodotBox/CheckedAbstractStorageTests` | A null `[NotNullStrict]` member fails `_Ready` through `GetDi()` |
| `World/ClientNetwork/EventDispatcherTests` | A received section reaches `ChatPresentation`, with a notice per entry; a throwing handler does not stop the batch; a broken section calls nothing; a private handler of a base class is found; a handler of a non-event type is rejected |
| `World/Composition/WorldServicesBuilderTests` | The world container builds for client, host, dedicated server with and without `ServerHud`; each gets its own services, created eagerly; queries in every one; the outbox and the command queue only on a server, the dedicated window only with `ServerHud`; the event dispatcher and the HUD mailbox wherever a Presentation is; chat commands registered on a server; a facade cycle is rejected |
| `World/Presentations/HudMailboxTests` | Notices of one frame read back by type in post order, not removed; a new frame clears them |
| `World/Protocol/NetMessageCodecTests` | Every command and event is mapped to a `ushort` id and round-trips; a not allowed, unknown or broken message is rejected; a section round-trips, a broken count is rejected |
| `World/Protocol/ProtocolHasherTests` | The protocol hash is stable for one type list and changes with one more type |
| `World/Simulations/ChatTests` | A player message goes to all, the window's as a server message; invalid text, line breaks and format characters included, is dropped; a command replies only to its sender; the admin gate; `Register` rejects a second call, a duplicate or bad name |
| `World/ServerNetwork/CommandDispatcherTests` | A command of a peer without a player is dropped; a false or throwing `Validate` drops only that command, a throwing `Process` stops nothing; join then chat in one tick both pass, a repeated join is dropped; a player handler of the join is rejected at `Register`; dedicated-window commands reach only their handler, which validates them too; the root's whitelist holds only commands |
| `World/ServerNetwork/CommandInboxTests` | A whitelisted packet comes out decoded with its peer; an event, a non-whitelisted, broken or over-long packet is dropped; arrival order across entry kinds |
| `World/ServerNetwork/EventOutboxTests` | Routing to all, to one player, to the dedicated window; the window never gets personal events, is absent headless; one peer keeps the order of publication |
| `World/ServerNetwork/PeerUidMapTests` | uid ↔ peerId lookup both ways, unbind, a uid or a peer bound twice throws |

## CI

`.github/workflows/build.yml` downloads the official Godot .NET build of the version the game builds with
(read from `Godot.NET.Sdk/<version>` in `NeonWarfare.csproj`), caches it, points `GODOT_BIN` at it and
runs this project in a step of its own.

## Known gotchas

* `gdUnit4.api` is a release candidate: running engine tests from a project other than the game's own
  arrived in 5.1, and the current stable adapter already requires it. Move to the stable release once it
  is out.
* The Godot version is written in two `.csproj` files — the game's and this one; the editor upgrades only
  the first. After a Godot upgrade, bump `Godot.NET.Sdk` here by hand.
