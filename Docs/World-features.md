# World features

[← Project README](../README.md)

`Src/NeonWarfare/Scenes/Worlds/Features/` holds the game itself, one folder per feature. A feature owns everything of
its own: its commands, events, notices, models, storages and services of every layer. Other features refer to its
state by uid or NetId and read it through its `[Query]`; a feature is never referred to from `Worlds/Infra/`.

## How a feature plugs in

Nothing is registered by hand: the composition root finds a feature by its types (see [World](World.md#layers)).

* **Services** — a class with a layer attribute (`[Simulation]`, `[SimulationFacade]`, `[Query]`,
  `[Presentation]`, …) is created in every World that has the layer. Its name ends with the layer: `*Simulation`,
  `*SimulationFacade`, `*Handler` for `[CommandHandler]`, `*Query`, `*Presentation`; a command, an event and a
  notice end with `Command`, `Event`, `Notice` (`WorldNamingTests`, both ways).
* **Commands** — a `Command` record with MessagePack keys and a `[CommandHandler]` implementing
  `IPlayerCommandHandler<T>`: the handler is what puts the command on the whitelist. The HUD sends it through
  `World.Send<T>`.
* **Events** — an `Event` record with MessagePack keys, published by a `[Simulation]` / `[SimulationFacade]` into
  `EventOutbox` and handled by an `[EventHandler]` of a `[Presentation]`. A one-frame signal to the HUD is a
  `Notice` the Presentation posts into `HudMailbox`.
* **State** — models (`[Replicated]` fields) live in entities, and the world-wide state of a feature in its own
  storage entities: one saved, one `[NotSaved]` for the session. `NewWorldSimulationFacade` spawns them in a new
  world; a save and the join snapshot bring them back. A `[Query]` finds a storage through
  `IEntityFinder.GetSingle<T>()` — see [Data and saves](Data-and-saves.md).
* **Chat commands** — a `[SimulationFacade]` named `*ChatCommandSimulationFacade` implementing `IChatCommand`, see
  [Chat and commands](Chat-and-commands.md).

## Folders

Every folder under `Worlds/Features/`, nested ones included. `WorldFeaturesDocTests` checks the table both ways.

| Folder | What it holds |
|---|---|
| `NewWorld` | `NewWorldSimulationFacade`: spawns what a new world starts with — the storages of every feature |
| `Players` | Players and their sessions: `PlayersStorage` (saved `PlayersModel` of `PlayerModel` by uid), `PlayersSessionStorage` (`[NotSaved]`, the online uids), join and leave (`PlayerSessionHandler` → `PlayerSimulationFacade`), `PlayerQuery`, `LocalPlayerPresentation` — passes this process's own `PlayerJoinedEvent` to `ILocalPlayerOwner` (the starter), which only then creates the UI; the admin flag granted by `WorldAdmin` is never revoked: it is saved |
| `Chat` | `SendChatMessageCommand` → `SendChatMessageHandler` → `ChatSimulationFacade` → `ChatSimulation`, the chat events, `ChatPresentation` with the history and `ChatEntryAddedNotice` |
| `Chat/ChatCommands` | `IChatCommand` and the chat commands, one class each |
| `Saves` | "Save as": `SaveCommand` (admin only) → `SaveCommandHandler` → `SaveSimulationFacade` → `SaveService`, the reply in the chat |
| `Surfaces` | The location nodes `Surface`, `SafeSurface`, `BattleSurface`; not spawned yet |
