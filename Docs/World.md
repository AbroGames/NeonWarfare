# World

[← Project README](../README.md)

The World (`Src/NeonWarfare/Scenes/Worlds/`) is one game world: its entities, the services working on them and the
network machinery that depends on the save. It is created by the game starter inside `Game` and dies with it, its
screen (`Hud` / `ServerHud`) with it. `Worlds/Infra/` is the machinery shared by every feature; the game itself lives
in `Worlds/Features/` — see [World features](World-features.md). The packets and flows are in
[Networking](Networking.md). The folder is plural so that its namespace does not share the name of the `World` class.

## The World node

`World` is a `Node2D` and the composition root. Its entry points:

| Member | Who calls it |
|---|---|
| `InitPreReady(setup, dependencies, origin)` | `Game.AddWorld`, before the World enters the tree: builds every service, then fills the world |
| `Send<TCommand>(command)` | The `Hud`, through `World.ICommandSender`: the only way a command leaves the World |
| `Get<T>()` | The screens, through `World.IReader`: only `[Query]` and `[Presentation]` services are handed out |
| `ReceiveFromClient`, `AddClient`, `RemoveClient` | `Game`, on a server: packets and connection events of the peers |
| `ReceiveFromServer` | `Game`, on a client: a state, events or join rejection packet |

`WorldOrigin` says where the state comes from: `NewWorld(saveFileName)` — `NewWorldSimulationFacade.Create()` spawns
what a world starts with; `FromSave(save, saveFileName)` — `SaveLoader`; `FromSnapshot(packet)` — the join snapshot,
on a remote client. A World is never empty: a remote client creates it only from the first snapshot. A broken save
or snapshot throws from `InitPreReady`, and the caller frees the World.

`WorldDependencies` is what `Game` hands to every World, none of it `null`: `TimeProvider`, `NetMessageCodec`,
`Replicator`, `FrameProvider`, `WorldPackedScenes`, `EntityCatalog`, both connections and `ILocalPlayerOwner`.
`WorldSetup` adds the ports of its configuration, see [Layers](#layers); the ones the owning process supplies are in
`Ports/`. With the Simulation the World also adds `ServerTickNode`, with the Server layer `SaveOnExitNode`.

## Layers

A service belongs to exactly one layer, set by its attribute. The starter chooses a configuration — a closed
`WorldSetup` record: a set of layers (`[Flags] enum WorldLayer`) and the ports that configuration needs. A layer brings
no other one with it. Only the composition root knows the setup: it is not in the container, and no service refers to
it (`LayerReferenceTests`), so a service sees its layer and the ports, never the configuration:

| `WorldSetup` | Ports | `WorldLayer` | Used by |
|---|---|---|---|
| `RemoteClient` | `LocalPlayer` | `Query \| Client \| Presentation \| ClientReplication` | A client connected to a remote server |
| `Host` | `ISaveFiles`, `LocalPlayer` (also the `WorldAdmin`), `IServerOwner` | `Dedicated`'s layers `\| Client \| Presentation` | Single player and hosting from inside the client |
| `Dedicated` | `ISaveFiles`, `WorldAdmin`, `IServerOwner` | `Simulation \| SimulationFacade \| CommandHandler \| Server \| Query` | A dedicated server, with `ServerHud` or without |

The host has no `ClientReplication`: its Simulation writes the very models its Presentation reads.

| Attribute | What lives there | The constructor may take | Configurations |
|---|---|---|---|
| `[Simulation]` | An optional leaf: one side effect (a model write, or an event with its log), no decisions; made only when several facades share it or it holds an invariant beyond the model (`ChatSimulation`, `EntitySpawner`) | `[Query]`, `[Server]` | Host, Dedicated |
| `[SimulationFacade]` | A finished operation: checks, decisions, order of steps; writes and publishes itself or through leaves (`PlayerSimulationFacade`) | `[Simulation]`, facades, `[Query]`, `[Server]` | Host, Dedicated |
| `[CommandHandler]` | `IPlayerCommandHandler<T>`, `IPeerSessionHandler`: validate and call a facade | Facades, `[Query]` | Host, Dedicated |
| `[Server]` | Tick loop, inbox and dispatcher, peers, outbox, replicator, saves, NetIds | `[Server]`, `[Query]` | Host, Dedicated |
| `[Query]` | Pure reads and calculations over models, no writes (`PlayerQuery`, `*StorageQuery`) | `[Query]` | All |
| `[Client]` | `EventDispatcher`, `PlayerCommandSender` | `[Client]`, `[Query]` | RemoteClient, Host |
| `[Presentation]` | Event handlers and what the HUD reads (`ChatPresentation`, `HudMailbox`) | `[Presentation]`, `[Query]` | RemoteClient, Host |
| `[ClientReplication]` | `StateApplier`: the state packets of a remote client | `[ClientReplication]`, `[Query]` | RemoteClient |

Every layer may also take the models, the `WorldDependencies` and the `WorldSetup` types, except those with an
effect beyond the world, which only their owner takes: `IClientsConnection`, `ISaveFiles` — `[Server]`;
`IServerOwner` — `[SimulationFacade]`; `IServerConnection` —
`[Client]`; `Replicator`, `EntityRecordReader` — `[Server]`, `[ClientReplication]`; `EntityRegistry`
and `WorldRoot` — the spawning layers, `[Simulation]` and `[ClientReplication]`; `LocalPlayer`,
`ILocalPlayerOwner` — `[Presentation]`.
`IEntityFinder` is open to all. `ConstructorLayerTests` checks all of it, the other `Architecture/` tests the rest:

* leaf simulations never call each other, a command handler calls only facades;
* only the Simulation writes models and publishes events, and only inside the tick;
* events reach only `[EventHandler]` methods of the Presentation, `HudMailbox.Post` is reached only from them;
* a Presentation exposes only property getters, and they change nothing (`HudMailbox` aside);
* no World service touches `Services.*` (`Di` aside): only the composition root does and passes what is needed;
* a service reads no entity in its constructor: when it is built, the world is still empty.

## The container

`WorldServicesBuilder.Build` (Microsoft.Extensions.DependencyInjection, `ValidateOnBuild`) registers the
`WorldDependencies`, the ports of the `WorldSetup`, the `WorldRoot`, and by hand `EntityRegistry` (also as
`IEntityFinder`) and `EntityRecordReader` — they belong to no single layer. Then it scans the layer attributes of the
assembly, keeps the classes of the selected layers and creates every one of them eagerly: a Presentation with only event
handlers is taken by no constructor. Last, it passes what MS.DI cannot inject: the Presentation to
`EventDispatcher.Register`, every `IChatCommand` to `ChatSimulationFacade.Register`, every `[CommandHandler]` to
`CommandHandlerRegistry.Register`. Adding a service is one class with a layer attribute; nothing else is edited. No
constructor of a service has an optional parameter: MS.DI would fill it silently. The GameTests build the container for
every configuration.

## Folders

Every folder of `Worlds/`, except the inside of `Features/`. `WorldDocTests` checks the table both ways.

| Folder | What it holds |
|---|---|
| `Features` | The game itself, one folder per feature — see [World features](World-features.md) |
| `Ports` | What the owning process supplies, everything with an effect beyond the World: `IClientsConnection` (implemented by `Game`, the host's own peer looped back synchronously), `IServerConnection` (a loopback on the host), `ISaveFiles`, `IServerOwner` (see [Shutdown](Shutdown.md)), `ILocalPlayerOwner`, `LocalPlayer` — who this process is, `WorldAdmin` — whose join grants `IsAdmin`; refers to nothing in `Features/` or the World root |
| `Infra` | The machinery shared by every feature; refers to nothing in `Features/` or the World root |
| `Infra/Composition` | `WorldLayer` — the layer flags; `WorldServiceAttribute` and one attribute per layer, see [Layers](#layers) |
| `Infra/Protocol` | The wire format: `Command` and `Event` bases (typing only), `NetMessageCodec` (a `ushort` type id and a MessagePack body) with `NetMessageFormatException`, `ProtocolHasher`, `ServerPacketKind` (the first byte of a server packet), `JoinRequestCommand`, `JoinRejectReason` and `JoinRejectedPacket` (the same layout in every build), `ColorFormatter` |
| `Infra/Server` | The `[Server]` layer, one folder per topic |
| `Infra/Server/Tick` | `ServerTickLoop` — one server tick, see [Networking](Networking.md#the-server-tick); `ServerTickNode` runs it last in the physics step |
| `Infra/Server/Commands` | `CommandInbox` (decoded on arrival) → `CommandDispatcher` (drains it in the tick); `CommandHandlerRegistry` — the handlers and the whitelist built from them; `IPlayerCommandHandler<TCommand>` |
| `Infra/Server/Peers` | `PeerGatekeeper` (handshake deadline, rejection, disconnection), `PeerSessions` (join, displacement, leave, in the tick), `PeerUidMap` (never replicated), `IPeerSessionHandler` |
| `Infra/Server/Events` | `EventOutbox` — the events of the tick, one buffer per joined peer |
| `Infra/Server/Replication` | `StateReplicator` — the state packet, the join snapshot and the save records, from the RepliCAT baselines |
| `Infra/Server/Saves` | `SaveService` ("save as" at the end of the tick, the save on exit through `SaveOnExitNode`), `SaveWriter`, `SaveLoader`, `SaveFormatException`, `SaveVersionMismatchException` |
| `Infra/Client` | `PlayerCommandSender` — the only sender in the World |
| `Infra/Client/Events` | `EventDispatcher` and `EventHandlerAttribute` |
| `Infra/Client/Replication` | `StateApplier` — the state packets and the join snapshot of a remote client |
| `Infra/Entities` | `NetId` (`NetId.None` is "nothing" and the World root), `NetIdGenerator`, `EntityRegistry` and its read side `IEntityFinder`, `EntityCatalog` (owned by `Game`), `EntitySpawner` — the only spawn on the server, `EntityRecordReader`, `WorldRoot`, `NotSavedAttribute` |
| `Infra/Presentation` | `HudMailbox` and `Notice` — one-frame notices from the Presentation to the HUD, never leaving the process |
