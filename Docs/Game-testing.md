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
| `Game/Networks/Transport/HostTransportTests` | The host's join reaches its World and the owner; the host's own peer is connected, then its join sent; its rejection reaches only the owner; its tick update of events goes to the World as events only, its disconnection to the World, a World failing on its events is not a failed send; each send to a remote peer goes to the network, without one throws; state and snapshot to the host's own peer throw |
| `Game/Networks/Transport/ServerTransportTests` | A peer's connection, disconnection and packets reach the World's server endpoint; `Dispose` detaches from the network |
| `Game/Networks/Transport/ClientTransportTests` | The join is sent on connect; before the World only the snapshot goes on, as its tick and records; a packet not from the server is dropped; a rejection reaches the owner; a broken packet, a broken snapshot header and a second snapshot break the connection once, and the later packets are dropped; after `Enter` a tick update reaches the World as its tick and sections, the state sections change its models; a broken tick update header breaks the connection without reaching the World; a section the World cannot apply breaks the connection once; a second `Enter` throws |
| `Game/Networks/Transport/ServerPacketsTests` | The byte layout of a tick update, a snapshot and a rejection; every kind reads back with its body; a tick update with state, events or both and a snapshot round-trip; a tick update with neither section is not written; a truncated header, a negative tick, a negative or overlong state length, no section, a truncated or negative snapshot tick, an empty packet, an unknown kind, a rejection body not one byte long are rejected; every reject reason, an unknown one too, reads back |
| `Game/GameProtocolTests` | The protocol hash is `ProtocolHasher` over the mapped types and the catalog's entity kinds |
| `Game/WorldAssemblerTests` | A single-player host World is built and its host joins in the next tick; a World that fails on a broken save or snapshot is freed, its server transport detached from the network |
| `Game/Starters/LastGameUpdatingSaveFilesTests` | A save written points the last game at it; a disk error throws and leaves it |
| `Screen/Hud/HudTests` | One chat line per `ChatEntry` subtype, the server nick and the join and leave facts translated |
| `Screen/ServerHud/ServerLogViewTests` | A record is shown as time, level, source, message and exception, its text as is; only new records are appended; over 1000 the oldest is removed with all its lines; a view far behind shows only the kept ones |
| `Scripts/GlobalServices/SaveLoadServiceTests` | A save replaces its file through a temporary one and a backup; a failed write or backup keeps the previous file; `Init` restores a save left only as its backup, removes an unfinished temporary file, keeps a file over its backup |
| `Scripts/GlobalServices/ServerLog/ServerLogServiceTests` | The last 1000 records in order; a read returns only the records after the version given; the message rendered with its properties, the level, source and exception kept; a read is a snapshot |
| `Scripts/PlayerUidTests` | A generated uid is valid and unique |
| `Worlds/Features/Chat/ChatTests` | A player message goes to all; invalid text, line breaks and format characters included, is dropped; a command replies only to its sender; the admin gate; a bound peer without a player loses only its own message; `/help` lists the listed commands by name and rights, not itself; the facade rejects a duplicate or bad name |
| `Worlds/Features/Saves/SaveCommandTests` | An admin's save is written and answered in chat; a disk error reaches the host's HUD as "failed" and keeps the previous file; a save from a player who leaves in the tick is still written; a non-admin's command and a bad name are dropped without a file or a reply |
| `Worlds/Features/Saves/SaveFileNameTests` | A save file name valid on every OS: no separators, no leading dot, no Windows device name in any case or with an extension; the length limit |
| `Worlds/Features/Players/PlayerJoinLeaveTests` | Join stores the player online, everyone gets the event, the joiner too; a returning player keeps nick and color; leave; the admin's leave reaches the server owner, on a dedicated server and on the host, its displacement does not; displacement by uid; every `Validate` rule rejects with its reason |
| `Worlds/Features/Players/PlayersStorageReplicationTests` | Both Players storages reach every client by delta and a late one by snapshot plus delta: players, their fields, the online set |
| `Worlds/Infra/Client/Events/EventDispatcherTests` | A received section reaches `ChatPresentation`, with a notice per entry; a throwing handler does not stop the batch; a broken section calls nothing; a private handler of a base class is found |
| `Worlds/Infra/Client/Tick/ClientTickClockTests` | A packet ahead moves the clock forward at once and cancels a lead, one equal to it cancels it too; one behind never moves it back, the next advances are skipped by the lead of the latest packet, then it counts again; a negative tick throws |
| `Worlds/Infra/Entities/EntityCatalogTests` | Kind ids: the scenes first, then the concrete node types in mapping order; a kind id of one catalog creates the same type in another built from the same build; unknown scene, type or id throw |
| `Worlds/Infra/Entities/EntityRegistryTests` | NetId ↔ node lookup, `Spawned`, `Despawned` once the node is out; `None`, a taken id or node rejected; removing or freeing a node or its ancestor takes it out; `GetAll` by class and base, in NetId order, a cached snapshot that never changes; `GetSingle` throws on none or several; `Exists` |
| `Worlds/Infra/Entities/EntitySpawnerTests` | A spawn from a scene or by node type goes under the root or under its parent entity with the next NetId; `initPreReady` runs before the tree, `Spawned` sees the node in the tree and the registry; an unknown parent, a kind outside the catalog, a scene of another root type, a throwing `initPreReady` creates nothing |
| `Worlds/Infra/Presentation/HudMailboxTests` | Notices of one frame read back by type in post order, not removed; a new frame clears them |
| `Worlds/Infra/Protocol/NetMessageCodecTests` | Every command and event is mapped to a `ushort` id and round-trips; a not allowed, unknown or broken message is rejected; a section round-trips, a broken count is rejected |
| `Worlds/Infra/Protocol/ProtocolCodecsTests` | `PlayerUid` and `NetId` are written as a bare string and integer and read back, by MessagePack and by RepliCAT; a default uid round-trips, a nil one reads as `default`; a negative or out-of-range NetId is rejected; `TickTimer` round-trips as its start and duration, a negative, out-of-range or overflowing one and a wrong array length are rejected |
| `Worlds/Infra/Protocol/ProtocolHasherTests` | A fixed input gives the recorded hash; the hash changes with one more type, one more, one fewer or a swapped entity kind |
| `Worlds/Infra/Server/Commands/CommandDispatcherTests` | A command of a not joined peer is dropped, of a bound one reaches the handler with its uid; a throwing player lookup in a handler drops only that command; a false or throwing `Validate` drops only that command, a throwing `Process` stops nothing; join then chat in one tick both pass; a displaced peer is ignored until its disconnection; a joined peer rejected by the inbox is ignored and leaves on its disconnection; nothing of a peer after its leave |
| `Worlds/Infra/Server/Commands/CommandHandlerRegistryTests` | Every handler interface of an object is found; the whitelist always holds the join; a second handler of one command and a bare `ICommandHandler` are rejected; the root's whitelist holds only commands and reaches the inbox |
| `Worlds/Infra/Server/Commands/CommandInboxTests` | A whitelisted packet comes out decoded with its peer; an event, a non-whitelisted, broken or over-long packet is dropped; a join of another protocol hash is rejected and disconnected; arrival order across entry kinds |
| `Worlds/Infra/Server/Events/EventOutboxTests` | Routing to all, to one player; one peer keeps the order of publication; a leaving or removed peer gets nothing |
| `Worlds/Infra/Server/Peers/PeerGatekeeperTests` | A peer not joined by the deadline is disconnected, a joined one is not; a rejection sends the reason before disconnecting, even a failed send disconnects; one transport disconnect per peer; a cut peer is not rejected again |
| `Worlds/Infra/Server/Peers/PeerSessionsTests` | A join makes the peer `Joined` before `Join`, a repeated one or one from a cut or never connected peer is dropped, a rejected or failed one is rolled back and disconnected with a neutral reason; displacement by uid with a `Displaced` leave, of a leaving peer with one `Leave`, the host never displaced; a peer disconnected by the server leaves only on its `peer_disconnected`; leave frees the uid even if its handler throws, a throwing displacement rejects the new peer |
| `Worlds/Infra/Server/Peers/PeerStateTableTests` | Every legal transition, an illegal one throws; the uid is held by a joined and a leaving peer, freed by `Release` and `Remove`; a disconnect is reported once; expired handshakes |
| `Worlds/Infra/Server/Replication/TickStateReplicationTests` | A model change reaches a remote client after the tick, before the events of that tick are handled; a peer joined in the tick gets no state section of it; one section for all, none for the host, none without changes, only changed entities, none of an entity despawned in the tick; the tick update and the snapshot carry the tick; a timer started before a join reads on the joiner as on the server; a broken state section or a delta the model cannot take is rejected; a failed send disconnects the peer, a failed rejection costs the other joiners nothing; a host World does not apply one |
| `Worlds/Infra/Server/Saves/SaveTests` | Save → load restores entities, models, the next NetId and the tick; a running timer reads as before the save and runs on; a `[NotSaved]` storage comes back empty; another protocol hash is a version mismatch, a broken save a format error; a failed requested save leaves the tick going on; a save of an entity whose delta failed throws until a delta succeeds; a save after the root left the tree is complete; a client joining a loaded world gets its entities |
| `Worlds/Infra/Server/Saves/SaveServiceTests` | Requested saves go to the new file at the end of the tick, one save for all of them, and it becomes the save file; a throwing callback does not stop the others; a disk or writer error goes to `failed` and keeps the file; the exit autosave writes the save file, not when disabled, not before the first tick, never throws |
| `Worlds/Infra/Server/Tick/ServerTickLoopTests` | An event of a tick reaches the host's `ChatPresentation` through loopback only at its end; every peer with events gets them, none without; a tick update carries state only, events only or both, the state never to the host; a step throwing after the state write still lets the peers get their state and propagates; a failed send disconnects only that peer; commands before sending; the handshake timeout after the commands; a peer disconnected by the server, before or inside the tick, gets nothing more |
| `Worlds/Infra/Tick/TickRateTests` | Seconds round to the nearest tick; a negative, NaN or too long time throws |
| `Worlds/Infra/Tick/TickTimerTests` | Elapsed, remaining, progress and finished before, during, at and after the span; `default` and a zero duration are finished; a negative or overflowing start throws |
| `Worlds/WorldServicesBuilderTests` | The world container builds for client, host, dedicated server; each gets its own services, created eagerly; queries in every one; the outbox and the command queue only on a server; the event dispatcher and the HUD mailbox wherever a Presentation is; chat commands only on a server; the state applier only on a remote client, the state replicator only on a server; a facade cycle is rejected; an `IEnumerable<I>` gets every service of `I` of the World's layers, a consumer rejecting it fails the build, a single `I` of several services and a service implementing a port are rejected; `Build` spawns nothing, `InitPreReady` of a new world spawns both Players storages |
| `Worlds/WorldEntryPointsTests` | A join through the server entry points reaches the host's `ChatPresentation` through loopback; a remote client's clock is the snapshot's tick, an event handler sees the tick of its update; one tick node per configuration advances its clock by one; `Send` loops back as the host's peer; an entry point without its layer throws, `Send` included; `Get` hands out only queries and Presentation of the World's layers; a World from a save has its models, from a foreign one throws; a broken state section stops the events of its tick update; a server World leaving the tree autosaves to the file of its setup, a loaded one too, a client one saves nothing |
| `Worlds/ReplicatedTypesTests` | Every game type with a `[Replicated]` member, nodes included, round-trips a delta and a snapshot with its default values |

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
