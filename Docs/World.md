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
`LocalPlayer` and `ILocalPlayerOwner` (`null` on a dedicated server), `WorldAdmin` (`null` on a remote client) and
`IDedicatedServerOwner` (only on a dedicated server). With the Simulation the World also adds `ServerTickNode`, with
the server network `SaveOnExitNode`.

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
`WorldDependencies`, the `WorldRoot`, and by hand `EntityRegistry` (also as `IEntityFinder`) and
`EntityRecordReader` — they belong to no single layer. Then it scans the layer attributes of the assembly, keeps the
classes of the selected layers and creates every one of them eagerly: a Presentation with only event handlers is
taken by no constructor. Last, it passes what MS.DI cannot inject: the Presentation to `EventDispatcher.Register`,
every `IChatCommand` to `ChatSimulationFacade.Register`, every `[CommandHandler]` to
`CommandHandlerRegistry.Register`. Adding a service is one class with a layer attribute; nothing else is edited. The
GameTests build the container for every configuration.

## Folders

Every folder of `World/`, except the inside of `Features/`. `WorldDocTests` checks the table both ways.

| Folder | What it holds |
|---|---|
| `Features` | The game itself, one folder per feature — see [World features](World-features.md) |
| `Infra` | The machinery shared by every feature; refers to nothing in `Features/` or the World root |
| `Infra/Composition` | `WorldLayer` — the layer flags and the three configurations; `WorldServiceAttribute` and one attribute per layer, see [Layers](#layers) |
| `Infra/Protocol` | The wire format: `Command` and `Event` bases (typing only), `NetMessageCodec` (a `ushort` type id and a MessagePack body) with `NetMessageFormatException`, `ProtocolHasher`, `ServerPacketKind` (the first byte of a server packet), `JoinRejectReason` and `JoinRejectedPacket` (the same layout in every build), `ColorFormatter` |
| `Infra/ServerNetwork` | The server side; `IClientsConnection` — the transport to the clients, implemented by `Game`, the host's own peer looped back synchronously; `IDedicatedServerOwner`, see [Shutdown](Shutdown.md) |
| `Infra/ServerNetwork/Tick` | `ServerTickLoop` — one server tick, see [Networking](Networking.md#the-server-tick); `ServerTickNode` runs it last in the physics step |
| `Infra/ServerNetwork/Commands` | `CommandInbox` (decoded on arrival) → `CommandDispatcher` (drains it in the tick); `CommandHandlerRegistry` — the handlers and the whitelist built from them; `IPlayerCommandHandler<TCommand>` |
| `Infra/ServerNetwork/Peers` | `PeerGatekeeper` (handshake deadline, rejection, disconnection), `PeerSessions` (join, displacement, leave, in the tick), `PeerUidMap` (never replicated), `JoinRequestCommand`, `IJoinRequestHandler` and `IPeerDisconnectedHandler` |
| `Infra/ServerNetwork/Events` | `EventOutbox` — the events of the tick, one buffer per joined peer |
| `Infra/ServerNetwork/Replication` | `StateReplicator` — the state packet, the join snapshot and the save records, from the RepliCAT baselines |
| `Infra/ServerNetwork/Saves` | `SaveService` ("save as" at the end of the tick, the save on exit through `SaveOnExitNode`), `SaveWriter`, `SaveLoader`, `ISaveFiles` (owned by the process), `SaveFormatException`, `SaveVersionMismatchException` |
| `Infra/ClientNetwork` | `IServerConnection` (a loopback on the host), `PlayerCommandSender` — the only sender in the World, `EventDispatcher` and `EventHandlerAttribute` |
| `Infra/ClientReplication` | `StateApplier` — the state packets and the join snapshot of a remote client |
| `Infra/Entities` | `NetId` (`NetId.None` is "nothing" and the World root), `NetIdGenerator`, `EntityRegistry` and its read side `IEntityFinder`, `EntityCatalog` (owned by `Game`), `EntitySpawner` — the only spawn on the server, `EntityRecordReader`, `WorldRoot`, `NotSavedAttribute` |
| `Infra/Hud` | `HudMailbox` and `Notice` — one-frame notices from the Presentation to the HUD, never leaving the process |
