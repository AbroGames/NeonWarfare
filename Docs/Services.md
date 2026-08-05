# Services

[← Project README](../README.md)

## Global services

The static `Services` class ([Scripts/Services.cs](../Scripts/Services.cs)), available from anywhere.
Some come from KludgeBox (`Di`, `Rand`, `Math`, `NodeTree`, `I18N`, `AutoScaling`, `AssemblyCache`,
`TypesMapping`, `ExceptionHandler`, `StringCompression`, `MembersScanner` — the last one proxies
`Di.MembersScanner`), some are game-specific:

| Service | Class | Purpose |
|---|---|---|
| `Services.Net` | `NetworkService` | The process role (`IsClient`/`IsServer`), the `DoClient`, `DoServerClient` helpers |
| `Services.MainScene` | `MainSceneService` | Switching MainMenu ↔ Game, the entry points into all the modes, `Shutdown()` |
| `Services.LoadingScreen` | `LoadingScreenService` | Showing / hiding the loading screen |
| `Services.GameSettings` | `GameSettingsService` | Client settings + the temporary `--nick` / `--uid` |
| `Services.DedicatedServerSettings` | `DedicatedServerSettingsService` | Dedicated server settings |
| `Services.MenuGameSettings` | `MenuGameSettingsService` | The bridge between `GameSettings` and the settings screen model |
| `Services.SaveLoad` | `SaveLoadService` | Save files, `SaveException` / `LoadException` |
| `Services.LastGame` | `ResumableGameService` | The last session for the "Continue" button (`ResumableGame`) |
| `Services.Process` | `ProcessService` | Launching the dedicated server child process |
| `Services.IconsStorage` | `IconsStorageService` | Icon identifiers |
| `Services.KnownServers` | `KnownServersService` | The server list of the multiplayer menu, stored in `user://known-servers.json` |

The field name in `Services` is deliberately shorter than the class name (`Services.SaveLoad` →
`SaveLoadService`): the `Service` suffix would be noise at the call site.

`Services.Di` and `Services.Net` are additionally exposed in `Services.Global` and pulled in through
`global using static` — which is why the code simply says `Di.Process(this)` and `Net.IsServer()`. Also
globally available there are `Consts.Global` (`ServerId`, `BroadcastId`) and the Godot extensions from
KludgeBox (vectors, colors, camera, nodes — `Vec2(x, y)`, for example, comes from there) — see
[Scripts/GlobalUsings.cs](../Scripts/GlobalUsings.cs). New global imports are added only there.

## World services

Child nodes of `World`. `World` itself implements `IServiceProvider` and registers them in a dictionary
by type in `_EnterTree()` — before `_Ready()` runs on the children. This is exactly what makes the
`[SceneService]` injection up the tree possible.

| Service | Purpose |
|---|---|
| `WorldServerStartStopService` | The server-side start: new game / loading, initialization of the synchronizer and the commands |
| `WorldClientStartStopService` | The client-side start: synchronization with the server, the loading screen, ping |
| `WorldSynchronizerService` | The client handshake, player validation, the initial world transfer |
| `WorldMultiplayerSpawnerService` | Attaching a `MultiplayerSpawner` to nodes |
| `WorldDataSaveLoadService` | Saving / loading, save permissions, autosave |
| `WorldDataSerializerService` | (De)serialization of `WorldPersistenceData` |
| `WorldChatService` | Chat, chat history, interceptors |
| `WorldCommandService` | Chat commands, automatic pickup of all `ICommandProcessor`s from the assembly |
| `WorldPlayerService` / `WorldEnemyService` | Spawning players and bots (a shared base, `WorldCharacterService`) |
| `WorldPerformanceService` | Godot / .NET / ENet / ping metrics |
| `WorldFacadeService` | A facade for frequent aggregate queries (player data, online/offline, `IsAdmin`) |

> [!NOTE]
> `World` is the service storage. Every service may reference other services and is a point of
> interaction with the system: by calling its method you should get a consistent state of the whole
> system, not just of that one service.

Adding a service means three edits at once, and nothing but a run will tell you that one was missed:
a field in `World.cs`, an entry in the list in `World._EnterTree()`, and a node in `World.tscn` with
the script attached and **named exactly like the field**.

The **node** names of these services in `World.tscn` go without the `World` prefix (`ChatService`, not
`WorldChatService`) — see [Scene tree](Scene-tree.md).
