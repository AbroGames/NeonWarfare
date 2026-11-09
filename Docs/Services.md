# Services

[← Project README](../README.md)

## Global services

The static `Services` class ([Services.cs](../Src/NeonWarfare/Scripts/Services.cs)), available from anywhere.
Some come from KludgeBox (`Di`, `Rand`, `Math`, `NodeTree`, `I18N`, `AutoScaling`, `AssemblyCache`,
`TypesMapping`, `ExceptionHandler`, `StringCompression`, `MembersScanner` — the last one proxies
`Di.MembersScanner`), some are game-specific:

| Service | Class | Purpose |
|---|---|---|
| `Services.Net` | `NetworkService` | The process role (`IsClient`/`IsServer`), the `DoClient`, `DoServerClient` helpers |
| `Services.MainScene` | `MainSceneService` | Switching MainMenu ↔ Game, the entry points into all the modes, `Shutdown()` |
| `Services.TerminationSignals` | `TerminationSignalsService` | SIGTERM / SIGINT → `MainScene.Shutdown()`, see [Shutdown](Shutdown.md) |
| `Services.LoadingScreen` | `LoadingScreenService` | Showing / hiding the loading screen |
| `Services.GameSettings` | `GameSettingsService` | Client settings + the temporary `--nick` / `--uid` |
| `Services.DedicatedServerSettings` | `DedicatedServerSettingsService` | Dedicated server settings |
| `Services.MenuGameSettings` | `MenuGameSettingsService` | The bridge between `GameSettings` and the settings screen model |
| `Services.SaveLoad` | `SaveLoadService` | Save files, `SaveException` / `LoadException`; the `ISaveFiles` of a server World |
| `Services.LastGame` | `ResumableGameService` | The last session for the "Continue" button (`ResumableGame`) |
| `Services.Process` | `ProcessService` | Launching the dedicated server child process |
| `Services.IconsStorage` | `IconsStorageService` | Icon identifiers |
| `Services.KnownServers` | `KnownServersService` | The server list of the multiplayer menu, stored in `user://known-servers.json` |

`NetworkService` and `TerminationSignalsService` extend abstract services of the same name from KludgeBox
(`KludgeBox.Godot.Services`): the base holds the mechanism, the game adds only its own part.

The field name in `Services` is deliberately shorter than the class name (`Services.SaveLoad` →
`SaveLoadService`): the `Service` suffix would be noise at the call site.

`Services.Di` and `Services.Net` are additionally exposed in `Services.Global` and pulled in through
`global using static` — which is why the code simply says `Di.Process(this)` and `Net.IsServer()`. Also
globally available there are `Consts.Global` (`ServerId`, `BroadcastId`) and the Godot extensions from
KludgeBox (vectors, colors, camera, nodes — `Vec2(x, y)`, for example, comes from there) — see
[GlobalUsings.cs](../Src/NeonWarfare/Scripts/GlobalUsings.cs). New global imports are added only there.

GodotBox does not use `Services`: it has its own `GodotBoxServices`,
with instances of its own and no global using — see [Code style conventions](Code-style.md#namespaces).

## World services

The services of a World are not global: the World builds them in its own container by their layer attributes, and
none of them reaches `Services.*` — see [World](World.md).
