# Networking

[← Project README](../README.md)

The **stock Godot high-level multiplayer** is used (`SceneMultiplayer` + `ENetMultiplayerPeer`).

## Main nodes

* **`Network`** ([Scenes/Game/Network/Network.cs](../Scenes/Game/Network/Network.cs)) — a wrapper over
  `MultiplayerApi`, responsible for `ConnectToServer()`, `HostServer()`, `OpenServer()` and a correct
  `Shutdown()`. It creates a new `SceneMultiplayer` bound to the `Game` node, so that on returning to
  the menu all the old subscriptions are guaranteed to fall away.
* **`NetworkStateMachine`** — the states `NotInitialized → Connecting/Hosting → Connected/Hosted →
  Disconnected` and the derived flags (`IsClient`, `IsServer`, `IsActiveGameState`).

## Process roles: `IsServer()` / `IsClient()`

| Mode | `Net.IsServer()` | `Net.IsClient()` |
|---|---|---|
| Main menu | `true` | `true` |
| Single-player game | `true` | `true` |
| Hosting "from inside the client" | `true` | `true` |
| Connecting to someone else's server | `false` | `true` |
| Dedicated server (`--server`) | `true` | `false` |

The [Startup flow](Startup-flow.md) describes how each mode is created.

## RPC

The main means of exchange. A "public wrapper + private `*Rpc`" pair:

```csharp
public void Save(string saveFileName) => RpcId(ServerId, MethodName.SaveRpc, saveFileName);
[Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
private void SaveRpc(string saveFileName) { /* ... */ }
```

No blank line between the two, since the public method is just a convenient alias for the private one.
The mode is always stated explicitly. The full cheat sheet is in the
[Code style conventions](Code-style.md).

## Spawning objects

`WorldMultiplayerSpawnerService.AddSpawnerToNode(node)` attaches a `WorldMultiplayerSpawner` (a
descendant of `AbstractMultiplayerSpawner` from KludgeBox) to a node. The spawner watches that node and
synchronizes over the network every sub-node created on it; it is named after the
`<node name>-MultiplayerSpawner` pattern and is deleted automatically together with the observed node.
The exception is the spawner for `Tree`, which sits right in `World.tscn` rather than being created from
code. Only the scenes listed in `SyncedPackedScenes` can be spawned.

## Field synchronization

The `[Sync]` attribute from `KludgeBox.Godot.Nodes.MpSync` (example:
`WorldTemporaryData.PlayerUidByPeerId`). What to sync via `[Sync]` and what to put into the save
snapshot is covered in [Data and saves](Data-and-saves.md).

## Transfer channels

`Consts.TransferChannel`: `Chat`, `StatsHp`, `StatsCache`. They are specified as
`[Rpc(TransferChannel = (int) Consts.TransferChannel.X)]` so that independent streams do not wait for
each other when a message is lost. The enum starts with `Default` — Godot's channel 0, which carries
all the remaining traffic; it is there so that none of our channels lands on it.

## Payload

Godot primitives or `byte[]` from MessagePack; JSON does not travel over the network. The maximum sync
packet size is `Network.MaxSyncPacketSize` = `1350 * 32` (about 43 KB: 32 ENet packets of one MTU
each). It limits `SceneMultiplayer` replication only — the size of an RPC is not capped, so the
initial world snapshot, which goes to the client in a single call, is not affected by it.

## Client connection

`WorldSynchronizerService`, identically for a networked and a single-player game. The client calls
`StartSyncOnClient(uid, nick, color)`, which reaches the server as `NewClientInitOnServerRpc`:
validation (uid, nickname length and so on), registration of `PlayerData`, granting admin rights. On
refusal the server sends `RejectSyncOnClientRpc(error)` and the client goes back to the menu with a
message; on success — `EndSyncOnClientRpc(byte[])` with all of `PersistenceData` in a single MessagePack
snapshot. The client deserializes the world and replies with `EndSyncOnServerRpc`, after which the
server calls `WorldPlayerService.SpawnPlayer(peerId)`.
