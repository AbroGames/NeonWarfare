# Scene tree

[← Project README](../README.md)

The project is built as a strict "container → contents" hierarchy. Each level knows only about its own
descendants; the contents are swapped through a `NodeContainer`.

```
Root (Node2D)                                      The entry point, lives for the whole application session
├── MainSceneContainer                             Holds MainMenu OR Game
│   └── MainMenu | Game
├── LoadingScreenContainer                         The loading screen on top of everything (CanvasLayer)
└── PackedScenes (RootPackedScenes)                Prototypes of the scenes created in Root: Game, MainMenu, LoadingScreen

Game (Node2D)                                      A single game session (single-player or networked)
├── WorldContainer → World                         The container holds the World
├── HudContainer                                   Holds Hud OR ServerHud
│   └── Hud | ServerHud
├── PackedScenes (GamePackedScenes)                Prototypes of the scenes created in Game: World, Hud, ServerHud
└── Network                                        Created from code, lives together with Game

World (Node2D, IServiceProvider)                   The game world and all of its services
├── Tree (WorldTree)                               The game tree with all the objects
│   └── Surface (SafeSurface | BattleSurface)      The current location
│       └── Character, Wall, ...                   Game objects, synchronized by MultiplayerSpawner
├── PersistenceData                                Data that goes into the save
├── TemporaryData                                  Data of the current session
├── Service                                        Services that live within a single game session
├── SyncedPackedScenes                             Prototypes of scenes that are synchronized from server to client on spawn
└── ClientPackedScenes                             Prototypes of purely client-side (visual) scenes
```

**The client and the server use the very same scene tree.** The role is determined at runtime through
`Net.IsServer()` / `Net.IsClient()` and through the `Net.DoClient(...)`, `Net.DoServerClient(...)`,
`Net.DoServerNotServer(...)` etc. helpers.

The control flow rule: **calls go down the tree, events (`event` / signals) go up.** The parent knows
about its children, the child does not know about its parent (the exception is an explicit `[Parent]`
injection in the world services).

## Node naming

The nodes of the world services in `World.tscn` are named **without the `World` prefix**: `ChatService`,
`PlayerService`, `SynchronizerService` — even though the classes are named `WorldChatService`,
`WorldPlayerService`, `WorldSynchronizerService`.

*A deliberate deviation from the "a scene and its handler are named the same" rule.* `[Child]` injects
**by field name**, so the node names must match the property names in `World.cs`, not the class names.
The `World` prefix in the class is needed to make the name unambiguous across the whole assembly; inside
`World.tscn` it would be noise (`World/WorldChatService`).

The practical consequence: **renaming a property in `World.cs` breaks the injection** until the node in
`World.tscn` is renamed too, and vice versa. The compiler does not catch this.
