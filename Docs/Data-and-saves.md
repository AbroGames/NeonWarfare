# Data and saves

[← Project README](../README.md)

The state of the world is its models. Nothing else is replicated or saved: the services keep no world state of
their own, and an event changes no model (see [Networking](Networking.md)).

## Models and storages

A model is a plain class with `[Replicated]` fields (RepliCAT), living in an entity node — `PlayerModel` inside
`PlayersModel` inside `PlayersStorage`. A model refers to other entities by `NetId` and to players by `PlayerUid`,
never to a node or a peer. Its fields are public; a private field behind a method is only for a consistency rule
(`PlayersModel.AddPlayer` keeps `PlayerByUid[uid].Uid == uid`). A plain collection is never `[Replicated]`: only the
RepliCAT ones (`ReplicatedDictionary`, `ReplicatedSet`, …).

The state of a whole feature lives in its own storage entities, spawned by `NewWorldSimulationFacade` in a new
world:

| Storage | Model | Goes into the save |
|---|---|---|
| `WorldOptionsStorage` | `WorldOptions`: the parameters the world was created with | Yes |
| `PlayersStorage` | `PlayersModel`: every player who ever joined, by uid | Yes |
| `OnlinePlayersStorage` | `OnlinePlayersModel`: the uids online now | No: `[NotSaved]` |

A `[NotSaved]` entity goes into the save without its state: after a load it is created empty from its kind and the
joins fill it again. So every replicated member of such a node is a readonly field set in its constructor
(`NotSavedEntityTests`). A service reads a storage through its `[Query]` (`PlayerQuery.PlayersModel`), which looks
the entity up on every call, so it works the same on the server, after a load and on a client.

A time in a model is a `TickTimer`: its start tick and its duration in ticks, read against `IWorldClock.CurrentTick`
with no reference to the clock. It is written once, at its start, and reads the same on the server, on a client from
its snapshot on, and after a load, which restores the tick counter. Seconds become ticks only through `TickRate`.

> [!IMPORTANT]
> Only the Simulation writes the models, and only inside the tick (`ModelRulesTests`, `SimulationTimingTests`). At
> the end of the tick `StateReplicator` sends the changes; a client only applies them. A write is cheap — only the
> last value of a tick travels — but a write outside the tick would miss both the clients and the save.

## Saves

A save is RepliCAT, as the join snapshot is: one path for both, written from the replicator's baselines, so it is
exactly the state at the end of a tick. `SaveWriter` writes the protocol hash (64 bits, first and fixed-size), the
next NetId and the tick, then a spawn record of every entity (`StateReplicator.WriteSave`).

* **"Save as"** — `SaveCommand` (admin) → `SaveService.RequestSave`: one `SaveWriter.Write()` serves every request
  of the tick, after its packets; the written file becomes the World's save file, and the sender gets a chat reply.
* **Autosave** — `SaveService.WriteRequested` also writes the save file every `AutoSaveIntervalMinutes` of
  `WorldSaving` (0 — never), counted from the first tick and from every write to the save file, so neither a load nor a
  "save as" is followed by an autosave at once. It goes before the requests of its tick, so a "save as" still moves the
  file. The starter fills the interval once per session from `GameSettings`, or from the dedicated server's settings.
* **On exit** — `SaveOnExitNode` calls `SaveService.SaveOnExit` when the World leaves the tree (quit or back to the
  menu), if a tick has run. It comes between ticks: `Quit()` only sets a flag. It and the autosave log how long the
  serialization and the disk write took.
* **Load** — `SaveLoader` (origin `FromSave`) spawns every entity with its saved NetId, then restores the NetId
  sequence and the tick counter. A save of another protocol hash throws `SaveVersionMismatchException`, a broken one
  `SaveFormatException`; the starter shows the player a message (see [Startup flow](Startup-flow.md)).

Any change to what the protocol hash covers (see [Networking](Networking.md#payload)) changes it: saves do not move
between versions of the game. The game is not released, so the compatibility of saves and of
the network protocol is broken freely: no migrations, no version shims.

## Files

All the user files live in the directory that Godot maps `user://` to. The directory depends on the OS
and on the project name (`Neon Warfare`):

| OS | Path |
|---|---|
| Windows | `%APPDATA%\Godot\app_userdata\Neon Warfare\` |
| Linux | `~/.local/share/godot/app_userdata/Neon Warfare/` |

Saves: `user://saves/<name>.bin`, the name of a new file is `yyyy-MM-dd_HH-mm` (`SaveLoadService`). Every name the
player gives passes `SaveFileName.IsValid`, the one rule for every OS.
The saves are described [above](#saves).

Other files in `user://` (JSON, `System.Text.Json`):

| File | What it stores |
|---|---|
| `game-settings.json` | Client settings |
| `player-uid.json` | The uid of the client's player, apart from the settings so that their fault never changes it |
| `dedicated-server-settings.json` | Dedicated server settings |
| `child-server-settings.json` | The settings of a child server, written by its client before the launch |
| `child-world-options.json` | The `WorldOptions` of a new world on a child server, written by its client before the launch |
| `resume-game.json` | The last session for the "Continue" button |
| `known-servers.json` | The server list of the multiplayer menu |

Every file in `user://` is written atomically through KludgeBox's `UserStorage`, and every JSON file is read through its
`JsonFile`, which keeps a corrupt file as `<name>.corrupt` and reads and writes Godot's `Color`, `Vector2`, `Vector3`
with no options, and an enum by its name only. The two settings files and the world options are read field by field
(`JsonFile.ReadOverDefault`, a nested object field by field too): a missing or bad field gets its default, and the
others stay.

`WorldOptions` is the one type that is both a JSON file and a model: the options travel to a child server as a file
and live in the world as its model, so one record serves both.
