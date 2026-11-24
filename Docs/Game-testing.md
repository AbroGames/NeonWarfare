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
| `GodotBox/AbstractStorageTests` | Only exported `PackedScene` properties are registered, after `_PreReady`; a scene's id is its index in the list |
| `GodotBox/CheckedAbstractStorageTests` | A null `[NotNullStrict]` member fails `_Ready` |
| `Screen/Hud/HudTests` | One chat line per `ChatEntry` subtype, the server nick and the join and leave facts translated; an unknown subtype throws |
| `Scripts/GlobalServices/SaveLoadServiceTests` | A save replaces its file through a temporary one and a backup; a failed write or backup keeps the previous file; a save interrupted between the renames is restored from the backup |
| `World/Features/Chat/ChatTests` | A player message goes to all; invalid text, line breaks and format characters included, is dropped; a command replies only to its sender; `/help` lists admin commands to an admin; the admin gate; a bound peer without a player loses only its own message; `Register` rejects a second call, a duplicate or bad name |
| `World/Features/Saves/SaveCommandTests` | An admin's save is written and answered in chat; a disk error reaches the host's HUD as "failed" and keeps the previous file; a save from a player who leaves in the tick is still written; a non-admin's command and a bad name are dropped without a file or a reply |
| `World/Features/Saves/SaveFileNameRuleTests` | A save file name valid on every OS: no separators, no leading dot, no Windows device name in any case or with an extension; the length limit |
| `World/Features/NewWorld/NewWorldSimulationFacadeTests` | A new world spawns both Players storages under the root |
| `World/Features/Players/PlayerJoinLeaveTests` | Join stores the player online, everyone gets the event, the joiner too; a returning player keeps nick and color; leave; displacement by uid; every `Validate` rule rejects with its reason; a uid of `UidGenerator` passes |
| `World/Features/Players/PlayersStorageQueryTests` | Each Players storage query returns the model of its storage; before the spawn it throws |
| `World/Features/Players/PlayersStorageReplicationTests` | Both Players storages reach every client by delta and a late one by snapshot plus delta: players, their fields, the online set |
| `World/Infra/ClientNetwork/EventDispatcherTests` | A received section reaches `ChatPresentation`, with a notice per entry; a throwing handler does not stop the batch; a broken section calls nothing; a private handler of a base class is found; a handler of a non-event type is rejected |
| `World/Infra/Entities/EntityCatalogTests` | Kind ids: the scenes first, then the concrete node types in mapping order; a kind id of one catalog creates the same type in another built from the same build; unknown scene, type or id throw |
| `World/Infra/Entities/EntityRegistryTests` | NetId ↔ node lookup, `SpawnedEvent`, `DespawnedEvent` once the node is out; `None`, a taken id or node rejected; removing or freeing a node or its ancestor takes it out; `GetAll` by class and base, in NetId order, a cached snapshot that never changes; `GetSingle` throws on none or several; `Exists` |
| `World/Infra/Entities/EntitySpawnerTests` | A spawn from a scene or by node type goes under the root or under its parent entity with the next NetId; `initPreReady` runs before the tree, `SpawnedEvent` sees the node in the tree and the registry; an unknown parent, a kind outside the catalog, a scene of another root type, a throwing `initPreReady` creates nothing |
| `World/Infra/Entities/NetIdGeneratorTests` | NetIds start at 1 and grow by one |
| `World/Infra/Hud/HudMailboxTests` | Notices of one frame read back by type in post order, not removed; a new frame clears them |
| `World/Infra/Protocol/NetMessageCodecTests` | Every command and event is mapped to a `ushort` id and round-trips; a not allowed, unknown or broken message is rejected; a section round-trips, a broken count is rejected |
| `World/Infra/Protocol/ProtocolHasherTests` | The protocol hash is stable for one type list and changes with one more type, one more, one fewer or a swapped entity kind |
| `World/Infra/Replication/TickStateReplicationTests` | A model change reaches a remote client after the tick, before the events of that tick are handled; a peer joined in the tick gets no state packet of it; one packet for all, none for the host, none without changes, only changed entities, none of an entity despawned in the tick; a broken state packet or a delta the model cannot take is rejected; a failed send disconnects the peer; a host World does not apply one |
| `World/Infra/ServerNetwork/Commands/CommandDispatcherTests` | A command of a not joined peer is dropped, of a bound one reaches the handler with its uid; a throwing player lookup in a handler drops only that command; a false or throwing `Validate` drops only that command, a throwing `Process` stops nothing; join then chat in one tick both pass; a displaced peer is ignored until its disconnection; nothing of a peer after its leave |
| `World/Infra/ServerNetwork/Commands/CommandHandlerRegistryTests` | Every handler interface of an object is found; the whitelist holds the join only with a join handler; a second handler of one command, join and leave handlers not in a pair, a player handler of the join, a second `Register` are rejected; the root's whitelist holds only commands and reaches the inbox |
| `World/Infra/ServerNetwork/Commands/CommandInboxTests` | A whitelisted packet comes out decoded with its peer; an event, a non-whitelisted, broken or over-long packet is dropped; a join of another protocol hash is rejected and disconnected; arrival order across entry kinds |
| `World/Infra/ServerNetwork/Events/EventOutboxTests` | Routing to all, to one player; one peer keeps the order of publication |
| `World/Infra/ServerNetwork/Peers/PeerGatekeeperTests` | A peer not joined by the deadline is disconnected, a joined one is not; a rejection sends the reason before disconnecting, even a failed send disconnects; one transport disconnect per peer; `Forget` clears the peer |
| `World/Infra/ServerNetwork/Peers/PeerSessionsTests` | A join binds the peer and its buffer before `Process`, a repeated one is dropped, a rejected or failed one is rolled back and disconnected with a neutral reason; displacement by uid, the host never displaced; leave unbinds even if its handler throws |
| `World/Infra/ServerNetwork/Peers/PeerUidMapTests` | uid ↔ peerId lookup both ways, unbind, a uid or a peer bound twice throws |
| `World/Infra/ServerNetwork/Saves/SaveTests` | Save → load restores entities, models, the next NetId and the tick; a `[NotSaved]` storage comes back empty; another protocol hash is a version mismatch, a broken save a format error; a requested save is written at the end of the tick, a failed one goes to `failed`; a save after the root left the tree is complete; a client joining a loaded world gets its entities |
| `World/Infra/ServerNetwork/Saves/SaveServiceTests` | A requested save goes to the new file at the end of the tick, which becomes the save file; a disk or writer error goes to `failed` and keeps the file; the exit autosave writes the save file, not when disabled, not before the first tick, never throws |
| `World/Infra/ServerNetwork/Tick/ServerTickLoopTests` | An event of a tick reaches the host's `ChatPresentation` through loopback only at its end; every peer with events gets an events packet, none without; a failing peer costs the others nothing; the tick counter; commands before sending; the handshake timeout after the commands |
| `World/WorldServicesBuilderTests` | The world container builds for client, host, dedicated server; each gets its own services, created eagerly; queries in every one; the outbox and the command queue only on a server; the event dispatcher and the HUD mailbox wherever a Presentation is; chat commands registered on a server; the state applier only on a remote client, the state replicator only on a server; a facade cycle is rejected; `Build` spawns nothing, `InitPreReady` of a new world spawns both Players storages |
| `World/WorldEntryPointsTests` | A join through the server entry points reaches the host's `ChatPresentation` through loopback; `Commands` loops back as the host's peer; a join rejection does not reach the event dispatcher, a broken packet throws; an entry point without its layer throws, `Commands` included; `Get` hands out only queries and Presentation of the World's layers; a World from a save has its models, from a foreign one throws; a server World leaving the tree autosaves to its file, a loaded one to the file it came from, a client one saves nothing |
| `World/ReplicatedTypesTests` | Every game type with a `[Replicated]` member, nodes included, round-trips a delta and a snapshot with its default values |

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
