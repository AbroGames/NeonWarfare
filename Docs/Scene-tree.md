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

World (Node2D)                                     The game world, its services are plain C# objects, see World.md
├── ServerTickNode                                 With the Simulation: runs the server tick
├── SaveOnExitNode                                 With the Server layer: the save on exit
└── PlayersStorage, PlayersSessionStorage, ...     The root entities, spawned by EntitySpawner
```

Which parts of the World exist on a client, a host or a dedicated server is decided by its layers, not
by runtime checks — see [World](World.md#layers).

The control flow rule: **calls go down the tree, events (`event` / signals) go up.** The parent knows
about its children, the child does not know about its parent. So `World` knows nothing about `Hud` and talks
to it through events only: not one of `World`'s own tasks needs the `Hud`.

The question to ask before adding a link: *does class X need class Y to do its own job?*
