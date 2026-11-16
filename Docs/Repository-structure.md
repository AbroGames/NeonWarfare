# Repository structure

[← Project README](../README.md)

```
Assets/                               Any files except scenes (.tscn) and code (.cs and .cs.uid)
├── Fonts/                            Fonts and their licenses
├── Locales/                          The localization template (.pot) and the locale files (.po)
├── Materials/                        Materials (.tres)
├── Resources/                        UI themes and other non-standard resources
├── Shaders/                          Shaders (.gdshader)
└── Textures/                         Textures (.png and .svg) and their sources (.psd)

Src/                                  Code and scenes of the Godot project; a namespace is the folder path relative to Src/
├── GodotBox/                         The game-independent layer over KludgeBox; must not depend on NeonWarfare
│   ├── GodotBoxServices.cs           GodotBox's own services (Di, Rand, MembersScanner), separate from the game's
│   └── Godot/
│       └── Nodes/                    NodeContainer, AbstractStorage, CheckedAbstractStorage, Background
│           ├── Camera/               Camera2D with pluggable shifts: shake, manual shake, punch
│           └── Process/              ProcessDeadChecker
└── NeonWarfare/                      The game itself
    ├── Scenes/                       Scenes (.tscn) and their handlers (.cs) — kept next to each other, in one folder
    │   ├── Root/                     The application entry point and the client and server starters
    │   ├── Game/                     The game session: a wrapper for the network and the game mode starters
    │   ├── World/                    The game world: one per game session, built from a set of layers
    │   │   ├── Infra/                Feature-independent machinery: layer attributes, entities and NetId, protocol, network, HUD mailbox; refers to nothing in Features/ or the World root
    │   │   └── Features/             One folder per feature with all its layers and models next to their nodes: Chat, Players, NewWorld…
    │   ├── Screen/                   UI: the main menu, HUD, server console, loading screen
    │   └── Old/                      Scenes kept from the old code for reuse: Character, Wall; no scripts
    └── Scripts/                      Code without scenes
        ├── Content/                  Input action names, loading screen types, cmd args; no reading or processing logic
        ├── GlobalServices/           Global services
        ├── Services.cs               The registry of all global services
        ├── Consts.cs                 Global constants
        └── GlobalUsings.cs           Here we declare global using and global using static

Tests/                                xUnit v3 tests
├── NeonWarfare.RepoTests/            A separate project, does not launch the engine
│   ├── Infrastructure/               Helpers for all the tests
│   ├── Docs/                         Tests that verify the documentation
│   ├── Localization/                 Tests over Assets/Locales/ and the keys used in the code
│   ├── Conventions/                  Static checks of the rules from Docs/Code-style.md and Docs/Cli-args.md, GodotBox independence
│   ├── Launch/                       Tests over Properties/launchSettings.json and .run/
│   ├── Repository/                   Repository-wide: size statistics, one Godot version across the projects
│   └── Scenes/                       Tests over the .tscn and .tres files
├── NeonWarfare.SmokeTests/           A separate project, launches the real game and reads its output
│   ├── Infrastructure/               Launching a process, capturing its output, scanning it for errors
│   └── Scenarios/                    The launch scenarios themselves
├── NeonWarfare.GameTests/            A separate Godot project, runs gdUnit4 tests of game nodes inside the engine
│   └── GodotBox/                     Tests of the GodotBox classes
└── .gdignore                         So that Godot does not look into this folder, since it holds no game code

Properties/
└── launchSettings.json               Quick-launch profiles for the game in different modes (Rider sees them by itself)
Docs/                                 Documentation, every file is linked from README.md
.run/                                 Rider Multi-Launch configurations: server + one or two clients at once
.github/                              CI: build, unit and game tests on a push into master and on a pull request (no smoke tests)
```
