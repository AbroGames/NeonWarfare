# Data and saves

[← Project README](../README.md)

The world data is split into two nodes and **contains no logic** — only state and its synchronization:

| Node | Lives | Synchronization | Goes into the save |
|---|---|---|---|
| `WorldPersistenceData` | until the end of the game | RPC inside the storage classes + a snapshot on connect | Yes |
| `WorldTemporaryData` | until the end of the session | `[Sync]` | No |

These are different nodes, and the choice between them is made deliberately: everything that has to
survive a restart goes into `PersistenceData`, everything else — into `TemporaryData`.

## World serialization

`WorldPersistenceData` consists of storage nodes (`GeneralDataStorage`, `PlayerDataStorage`), each of
which implements `ISerializableStorage` (`SerializeStorage` / `DeserializeStorage` /
`SetAllPropertyListeners`).

`WorldDataSerializerService` walks all the `ISerializableStorage` instances inside
`WorldPersistenceData` by reflection (`Services.MembersScanner`) and assembles a
`Dictionary<string, byte[]>` → MessagePack. The same byte blob is used both for saving to disk and for
the initial synchronization of a new client (see [Networking](Networking.md)).

## Models

The models themselves (`PlayerData`, `GeneralData`) are `ObservableObject`s with `[ObservableProperty]`
and MessagePack `[Key(N)]` keys. The storage subscribes to `PropertyChanged` and automatically
broadcasts the change to the clients.

> [!IMPORTANT]
> **Any write to a model property on the server is a network operation in itself.** These properties
> must not be used as working variables in loops: every assignment generates traffic.

## Files

All the user files live in the directory that Godot maps `user://` to. The directory depends on the OS
and on the project name (`Neon Warfare`):

| OS | Path |
|---|---|
| Windows | `%APPDATA%\Godot\app_userdata\Neon Warfare\` |
| Linux | `~/.local/share/godot/app_userdata/Neon Warfare/` |

Saves: `user://saves/<name>.bin`, the name of a new file is `yyyy-MM-dd_HH-mm` (`SaveLoadService`).
Autosave happens in `WorldServerShutdowner` on leaving the tree, under the `AutoSaveEnabled` setting —
see [Shutdown](Shutdown.md).

Other files in `user://` (JSON, `System.Text.Json`):

| File | What it stores |
|---|---|
| `game-settings.json` | Client settings |
| `dedicated-server-settings.json` | Dedicated server settings |
| `resume-game.json` | The last session for the "Continue" button |
