# World

[← Project README](../README.md)

The World (`Src/NeonWarfare/Scenes/World/`) is one game world: its entities, the services working on them and the
network machinery that depends on the save. It is created by the game starter inside `Game` and dies with it, its
screen (`Hud` / `ServerHud`) with it. `World/Infra/` is the machinery shared by every feature; the game itself lives
in `World/Features/` — see [World features](World-features.md). The packets and flows are in
[Networking](Networking.md).

## The World node

`World` is a `Node2D` and the composition root. Its entry points:

| Member | Who calls it |
|---|---|
| `InitPreReady(layers, dependencies, origin)` | `Game.AddWorld`, before the World enters the tree: builds every service, then fills the world |
| `Send<TCommand>(command)` | The `Hud`, through `World.ICommandSender`: the only way a command leaves the World |
| `Get<T>()` | The screens, through `World.IReader`: only `[Query]` and `[Presentation]` services are handed out |
| `ReceiveFromClient`, `OnClientConnected`, `OnClientDisconnected` | `Game`, on a server: packets and connection events of the peers |
| `ReceiveFromServer` | `Game`, on a client: a state, events or join rejection packet |

`WorldOrigin` says where the state comes from: `NewWorld(saveFileName)` — `NewWorldSimulationFacade.Create()` spawns
what a world starts with; `FromSave(save, saveFileName)` — `SaveLoader`; `FromSnapshot(packet)` — the join snapshot,
on a remote client. A World is never empty: a remote client creates it only from the first snapshot. A broken save
or snapshot throws from `InitPreReady`, and the caller frees the World.

`WorldDependencies` is what `Game` hands to every service: `TimeProvider`, `NetMessageCodec`, `Replicator`,
`FrameProvider`, `WorldPackedScenes`, `EntityCatalog`, both connections, `ISaveFiles` (`null` on a remote client),
`LocalPlayer` (`null` on a dedicated server), `WorldAdmin` (`null` on a remote client) and `IDedicatedServerOwner`
(only on a dedicated server). With the Simulation the World also adds `ServerTickNode`, with the server network
`SaveOnExitNode`.

## Layers

A service belongs to exactly one layer, set by its attribute. The starter passes the set of layers
(`[Flags] enum WorldLayer`), never a role; a layer brings no other one with it:

| Configuration | `WorldLayer` | Used by |
|---|---|---|
| `Client` | `Query \| ClientNetwork \| Presentation \| ClientReplication` | A client connected to a remote server |
| `Host` | `Dedicated \| Client` without `ClientReplication` | Single player and hosting from inside the client |
| `Dedicated` | `Simulation \| SimulationFacade \| CommandHandler \| ServerNetwork \| Query` | A dedicated server, with `ServerHud` or without |

The host has no `ClientReplication`: its Simulation writes the very models its Presentation reads.

| Attribute | What lives there | The constructor may take | Configurations |
|---|---|---|---|
| `[Simulation]` | A leaf: one side effect (a model write, or an event with its log), no decisions (`ChatSimulation`) | `[Query]`, `[ServerNetwork]` | Host, Dedicated |
| `[SimulationFacade]` | A finished operation: checks, decisions, order of steps (`PlayerSimulationFacade`) | `[Simulation]`, facades, `[Query]`, `[ServerNetwork]` | Host, Dedicated |
| `[CommandHandler]` | `IPlayerCommandHandler<T>`, `IJoinRequestHandler`, `IPeerDisconnectedHandler`: validate and call a facade | Facades, `[Query]` | Host, Dedicated |
| `[ServerNetwork]` | Tick loop, inbox and dispatcher, peers, outbox, replicator, saves, NetIds | `[ServerNetwork]`, `[Query]` | Host, Dedicated |
| `[Query]` | Pure reads and calculations over models, no writes (`PlayerQuery`, `*StorageQuery`) | `[Query]` | All |
| `[ClientNetwork]` | `EventDispatcher`, `PlayerCommandSender` | `[ClientNetwork]`, `[Query]` | Client, Host |
| `[Presentation]` | Event handlers and what the HUD reads (`ChatPresentation`, `HudMailbox`) | `[Presentation]`, `[Query]` | Client, Host |
| `[ClientReplication]` | `StateApplier`: the state packets of a remote client | `[ClientReplication]`, `[Query]` | Client |

Every layer may also take the models and the `WorldDependencies` types, except those with an effect beyond the
world, which only their owner takes: `IClientsConnection`, `ISaveFiles` — `[ServerNetwork]`;
`IDedicatedServerOwner` — `[SimulationFacade]`; `IServerConnection` —
`[ClientNetwork]`; `Replicator`, `EntityRecordReader` — `[ServerNetwork]`, `[ClientReplication]`; `EntityRegistry`
and `WorldRoot` — the spawning layers, `[Simulation]` and `[ClientReplication]`; `LocalPlayer` — `[Presentation]`.
`IEntityFinder` is open to all. `ConstructorLayerTests` checks all of it, the other `Architecture/` tests the rest:

* leaf simulations never call each other, a command handler calls only facades;
* only the Simulation writes models and publishes events, and only inside the tick;
* events reach only `[EventHandler]` methods of the Presentation, `HudMailbox.Post` is reached only from them;
* a Presentation exposes only property getters, and they change nothing (`HudMailbox` aside);
* no World service touches `Services.*` (`Di` aside): only the composition root does and passes what is needed;
* a service reads no entity in its constructor: when it is built, the world is still empty.

## The container

`WorldServicesBuilder.Build` (Microsoft.Extensions.DependencyInjection, `ValidateOnBuild`) registers the
`WorldDependencies`, the `WorldRoot`, and by hand `EntityRegistry` (also as `IEntityFinder`) and
`EntityRecordReader` — they belong to no single layer. Then it scans the layer attributes of the assembly, keeps the
classes of the selected layers and creates every one of them eagerly: a Presentation with only event handlers is
taken by no constructor. Last, it passes what MS.DI cannot inject: the Presentation to `EventDispatcher.Register`,
every `IChatCommand` to `ChatSimulationFacade.Register`, every `[CommandHandler]` to
`CommandHandlerRegistry.Register`. Adding a service is one class with a layer attribute; nothing else is edited. The
GameTests build the container for every configuration.

## Inventory

Every top-level type of `World/*.cs` and `World/Infra/`, except the `Command`, `Event` and `Notice` records and the
models. `WorldDocTests` checks the tables both ways.

### Root

| Type | What it is |
|---|---|
| `World` | The World node and composition root, see [above](#the-world-node) |
| `WorldDependencies` | What `Game` hands to every service |
| `WorldOrigin` | New, from a save or from the join snapshot |
| `WorldPackedScenes` | The scenes the World spawns; their order is the scene part of `EntityCatalog`, owned by `Game` |
| `WorldServicesBuilder` | Builds the container; separate from `World` so a test can build every configuration |

### Composition

| Type | What it is |
|---|---|
| `WorldLayer` | The layer flags and the three configurations |
| `WorldServiceAttribute` | The base of the layer attributes, holds the `WorldLayer` |
| `SimulationAttribute`, `SimulationFacadeAttribute`, `CommandHandlerAttribute`, `ServerNetworkAttribute`, `QueryAttribute`, `ClientNetworkAttribute`, `PresentationAttribute`, `ClientReplicationAttribute` | One per layer, see [Layers](#layers) |

### Protocol

| Type | What it is |
|---|---|
| `Command` | The base of every command, for typing only: the whitelist comes from the handlers |
| `Event` | The base of every event, for typing only: the client reads only the known event types |
| `NetMessageCodec` | A message is a `ushort` type id and a MessagePack body; sections of several; the protocol hash |
| `NetMessageFormatException` | A packet that cannot be read |
| `ProtocolHasher` | The hash of the mapped types, MessagePack keys, RepliCAT schemas and entity kinds |
| `ServerPacketKind` | The first byte of a server packet |
| `JoinRejectReason` | Why a join is refused, a code the client shows in its own language |
| `JoinRejectedPacket` | The layout of the `JoinRejected` packet, the same in every build |
| `ColorFormatter` | MessagePack formatter of Godot `Color` |

### ServerNetwork

| Type | What it is |
|---|---|
| `IClientsConnection` | The transport to the clients, implemented by `Game`; the host's own peer is looped back synchronously |
| `IDedicatedServerOwner` | `AdminLeft()`, implemented by `DedicatedServerGameStarter`, see [Shutdown](Shutdown.md) |
| `ServerTickLoop` | One server tick, see [Networking](Networking.md#the-server-tick); the tick counter |
| `ServerTickNode` | Calls `RunTick` last in the physics step |
| `CommandInbox` | The commands and disconnections since the last tick, decoded on arrival |
| `CommandDispatcher` | Drains the inbox in the tick and routes every entry |
| `CommandHandlerRegistry` | Every network handler and the command whitelist built from them |
| `IPlayerCommandHandler<TCommand>` | `Validate` and `Process` of a command from a joined player, by uid |
| `IJoinRequestHandler` | Validates and processes a join; the peer has no player yet |
| `IPeerDisconnectedHandler` | The pair of the join: a joined peer leaves or is displaced |
| `PeerGatekeeper` | Handshake deadline, rejection, disconnection of peers |
| `PeerSessions` | Join, displacement and leave of a peer, in the tick |
| `PeerUidMap` | Peer ↔ uid of the joined players, never replicated |
| `EventOutbox` | The events of the tick, one buffer per joined peer |
| `StateReplicator` | The state packet, the join snapshot and the save records, from the RepliCAT baselines |
| `SaveService` | The save file of the World, "save as" at the end of the tick, the save on exit |
| `SaveWriter` | Writes the save: protocol hash, next NetId, tick, records |
| `SaveLoader` | Fills a new World from a save |
| `SaveOnExitNode` | Saves when the World leaves the tree |
| `ISaveFiles` | The save files on disk and the autosave setting, owned by the process |
| `SaveFormatException`, `SaveVersionMismatchException` | A broken save, a save of another protocol hash |

### ClientNetwork

| Type | What it is |
|---|---|
| `IServerConnection` | The transport to the server, implemented by `Game`; on the host a loopback |
| `PlayerCommandSender` | Encodes a command and sends it: the only sender in the World |
| `EventDispatcher` | Reads an events packet and calls the `[EventHandler]` methods |
| `EventHandlerAttribute` | Marks a private `Handle(TEvent)` of a Presentation |

### ClientReplication

| Type | What it is |
|---|---|
| `StateApplier` | Applies a state packet and the join snapshot on a remote client |

### Entities

| Type | What it is |
|---|---|
| `NetId` | The network identity of an entity; `NetId.None` is "nothing" and the World root |
| `NetIdGenerator` | Hands out NetIds in order, restored from a save |
| `EntityRegistry` | NetId ↔ node ↔ kind of every spawned entity |
| `IEntityFinder` | The read side of the registry, open to every layer |
| `EntityCatalog` | The kind id of every entity: scenes, then node types; owned by `Game` |
| `EntitySpawner` | The only spawn on the server, and the despawn |
| `EntityRecordReader` | Spawns from records with their NetIds: snapshot, state packet, save |
| `WorldRoot` | The node a root entity is added to, instead of the whole `World` |
| `NotSavedAttribute` | An entity the save keeps without its state |

### Hud

| Type | What it is |
|---|---|
| `HudMailbox` | One-frame notices from the Presentation to the HUD |
| `Notice` | The base of every notice; never leaves the process |
