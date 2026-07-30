# Repository structure

[← Project README](../README.md)

```
Assets/                       Any files except scenes (.tscn) and code (.cs and .cs.uid)
├── Fonts/                    Fonts and their licenses
├── Locales/                  The localization template (.pot) and the locale files (.po)
├── Resources/                UI themes and other non-standard resources
├── Shaders/                  Shaders (.gdshader)
└── Textures/                 Textures (.png and .svg) and their sources (.psd)

Scenes/                       Scenes (.tscn) and their handlers (.cs) — kept next to each other, in one folder
├── Root/                     The application entry point and the client and server starters
├── Game/                     The game session: a wrapper for the network and the game mode starters
├── World/                    The world: one per game session, synchronized from the server to the client
│   ├── Data/                 World data
│   │   ├── PersistenceData/  Data that goes into the save (General, Player)
│   │   └── TemporaryData/    Data of the current session, not saved
│   ├── Scenes/               Prototypes of the scenes available for spawning
│   │   ├── ClientScenes/     Purely client-side (visual) scenes
│   │   └── SyncedScenes/     Scenes that, when spawned on the server, will be synchronized to the clients
│   ├── Service/              World services: chat, commands, characters, spawning, performance, start/stop
│   └── Tree/                 The current surface (location) and the game objects on it
│       └── Surfaces/         Surfaces (locations): Safe, Battle
├── Entity/                   Game objects: characters (controllers, stats, effects), walls
├── Screen/                   UI: the main menu, HUD, server console, loading screen
└── KludgeBox/                Thin descendants of the KludgeBox nodes

Scripts/                      Code without scenes
├── Content/                  Game entity stats, loading screen types, command-line arguments
├── Service/                  Global services
├── Services.cs               The registry of all global services
├── Consts.cs                 Global constants
└── GlobalUsings.cs           Here we declare global using and global using static

Tests/                        xUnit v3 tests
├── NeonWarfare.Tests/        A separate project, does not launch the engine
│   ├── Infrastructure/       Helpers for all the tests
│   └── Docs/                 Tests that verify the documentation
└── .gdignore                 So that Godot does not look into this folder, since there are only unit tests here

Properties/
└── launchSettings.json       Quick-launch profiles for the game in different modes (Rider sees them by itself)
Docs/                         Documentation, every file is linked from README.md
.run/                         Rider Multi-Launch configurations: server + one or two clients at once
```
