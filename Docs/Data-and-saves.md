# Data and saves

[← Project README](../README.md)

The state of the world is its models. Nothing else is replicated or saved: the services keep no world state of
their own, and an event changes no model (see [Networking](Networking.md)).

## Models and storages

A model is a plain class with `[Replicated]` fields (RepliCAT), living in an entity node — `PlayerModel` inside
`PlayersModel` inside `PlayersStorage`. A model refers to other entities by `NetId` and to players by uid, never to a
node or a peer. Its fields are public; a private field behind a method is only for a consistency rule
(`PlayersModel.AddPlayer` keeps `PlayerByUid[uid].Uid == uid`). A plain collection is never `[Replicated]`: only the
RepliCAT ones (`ReplicatedDictionary`, `ReplicatedSet`, …).

The state of a whole feature lives in its own storage entities, spawned by `NewWorldSimulationFacade` in a new
world:

| Storage | Model | Goes into the save |
|---|---|---|
| `PlayersStorage` | `PlayersModel`: every player who ever joined, by uid | Yes |
| `PlayersSessionStorage` | `PlayersSessionModel`: the uids online now | No: `[NotSaved]` |

A `[NotSaved]` entity goes into the save without its state: after a load it is created empty from its kind and the
joins fill it again. So every replicated member of such a node is a readonly field set in its constructor
(`NotSavedEntityTests`). A service reads a storage through its `[Query]` (`PlayersStorageQuery.Model`), which looks
the entity up on every call, so it works the same on the server, after a load and on a client.

> [!IMPORTANT]
> Only the Simulation writes the models, and only inside the tick (`ModelRulesTests`, `SimulationTimingTests`). At
> the end of the tick `StateReplicator` sends the changes; a client only applies them. A write is cheap — only the
> last value of a tick travels — but a write outside the tick would miss both the clients and the save.

## Saves

A save is RepliCAT, as the join snapshot is: one path for both, written from the replicator's baselines, so it is
exactly the state at the end of a tick. `SaveWriter` writes the protocol hash (64 bits, first and fixed-size), the
next NetId and the tick, then a spawn record of every entity (`StateReplicator.WriteSave`).

* **"Save as"** — `SaveCommand` (admin) → `SaveService.RequestSave`: the save is written at the end of the tick, after
  its packets; the written file becomes the World's save file, and the sender gets a chat reply.
* **On exit** — `SaveOnExitNode` calls `SaveService.SaveOnExit` when the World leaves the tree (quit or back to the
  menu), under the `AutoSaveEnabled` setting, if a tick has run. It comes between ticks: `Quit()` only sets a flag.
* **Load** — `SaveLoader` (origin `FromSave`) spawns every entity with its saved NetId, then restores the NetId
  sequence and the tick counter. A save of another protocol hash throws `SaveVersionMismatchException`, a broken one
  `SaveFormatException`; the starter shows the player a message (see [Startup flow](Startup-flow.md)).

Any change to the protocol — a mapped type, a MessagePack key, a replicated member, an entity kind — changes the
hash: saves do not move between versions of the game.

## Files

All the user files live in the directory that Godot maps `user://` to. The directory depends on the OS
and on the project name (`Neon Warfare`):

| OS | Path |
|---|---|
| Windows | `%APPDATA%\Godot\app_userdata\Neon Warfare\` |
| Linux | `~/.local/share/godot/app_userdata/Neon Warfare/` |

Saves: `user://saves/<name>.bin`, the name of a new file is `yyyy-MM-dd_HH-mm` (`SaveLoadService`).
The save on exit and "save as" are described [above](#saves).

Other files in `user://` (JSON, `System.Text.Json`):

| File | What it stores |
|---|---|
| `game-settings.json` | Client settings |
| `dedicated-server-settings.json` | Dedicated server settings |
| `resume-game.json` | The last session for the "Continue" button |
| `known-servers.json` | The server list of the multiplayer menu |
