# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | C# (with Godot `.tscn` scene files) | `NeonWarfare.csproj:1`, `project.godot:19` |
| Runtime + version | .NET `net10.0` (target framework) | `NeonWarfare.csproj:3` |
| Game engine + version | Godot 4.7, C# build, Forward Plus renderer | `project.godot:19` (`PackedStringArray("4.7", "C#", "Forward Plus")`), `NeonWarfare.csproj:1` (`Godot.NET.Sdk/4.7.1`) |
| Package manager | NuGet (via MSBuild `<PackageReference>`) | `NeonWarfare.csproj:7-11` |
| Module/build system | MSBuild / `Godot.NET.Sdk` SDK; assembly name `NeonWarfare` | `NeonWarfare.csproj:1`, `project.godot:22-24` |
| Solution file | Visual Studio 2012 format, single project `NeonWarfare` | `NeonWarfare.sln` |

> [!NOTE]
> The README states "latest" versions for both Godot and .NET. The actual pinned values are **Godot 4.7** and **.NET 10.0** (`net10.0`) — this is an Intent vs. Reality divergence (see CONCERNS.md).

### 2) Production Frameworks and Dependencies

Only three production NuGet packages are declared — the project is otherwise self-contained plus the internal KludgeBox library.

| Dependency | Version | Role in system | Evidence |
|------------|---------|----------------|----------|
| `Godot.NET.Sdk` | 4.7.1 (SDK) | Godot C# bindings, build integration, scene/node base types | `NeonWarfare.csproj:1` |
| `KludgeBox` | 3.3.3 | In-house framework: DI container (`Di`, `[Child]`/`[Parent]`/`[SceneService]`/`[Logger]`), logging (Serilog wrapper), global `Services`, networking utilities, Godot node extensions, `NodeContainer`, `AbstractMultiplayerSpawner`, `MpSync`, stat modifiers, `ProcessDeadChecker`/`ProcessShutdowner` | `NeonWarfare.csproj:8` |
| `CommunityToolkit.Mvvm` | 8.4.0 | `[ObservableProperty]` source generator for world data models (`PlayerData`, `GeneralData`) — generates `INotifyPropertyChanged` properties that storages subscribe to for network sync | `NeonWarfare.csproj:9`, `PlayerData.cs:1,8` |
| `MessagePack` | 3.1.4 | Binary serialization of world state for network payloads (`byte[]`) and on-disk saves; `[MessagePackObject]`, `[Key(N)]`, `[IgnoreMember]` | `NeonWarfare.csproj:10`, `IController.cs:11`, `PlayerData.cs:7` |

Transitively pulled in (via KludgeBox): **Serilog** (logging backend, referenced through `[Logger] ILogger`), **Humanizer** (string humanization, e.g. `setting.Member.Name.Humanize().Titleize()` in `Setting.cs:22`, and `FormatWith(...)` for error templates). These are not declared directly in `NeonWarfare.csproj`.

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| Godot .NET editor | Scene editing, project run, export presets, locale `.po` files | `project.godot`, `export_presets.cfg` |
| Rider / VS | Recommended C# IDE; `.sln`/`.csproj` + `.idea/` + `*.DotSettings.user` present | `README.md:51-53`, `NeonWarfare.sln`, `NeonWarfare.sln.DotSettings.user` |
| [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases) | Sets up `launchSettings.json` ENV vars for Godot path | `README.md:50-51` |
| `.editorconfig` | Code formatting: UTF-8, LF, max line length 120 for `*.cs` | `.editorconfig:1-5` |

No linter/formatter beyond `.editorconfig` is configured. No CI/CD pipeline, container, security scanning, or performance-testing config is present (scan output: "No CI/CD pipelines detected", "No containerization configs detected", "No security configs detected").

### 4) Key Commands

There is no `dotnet test`/`lint` project. The project is built and run through Godot or the .NET solution.

```bash
# Build (from project root)
dotnet build NeonWarfare.sln            # restore NuGet + compile assembly "NeonWarfare"

# Run via Godot editor: open project.godot, press F5 (starts Root.tscn)
# Run headless/dedicated server
dotnet run --project NeonWarfare.csproj -- --server --headless --port 25566

# Multiplayer fast-test (README §Quick Start): Rider run configs, e.g.
#   Server          : --server
#   Autoconnect (1) : --auto-connect --uid TestPlayer1 --nick TestPlayer1
#   Autoconnect (2) : --auto-connect --uid TestPlayer2 --nick TestPlayer2
```

No automated test command exists (see TESTING.md — there are no tests).

### 5) Environment and Config

- **Config sources:**
  - `project.godot` — engine config (main scene, physics, input maps, locales, renderer, clear color).
  - `.editorconfig` — code style.
  - `export_presets.cfg` — Godot export presets (Windows/Linux).
  - `user://` runtime files (JSON): `game-settings.json`, `dedicated-server-settings.json`, `resume-game.json`; binary saves under `user://saves/<name>.bin` (`SaveLoadService.cs:18-20`).
- **Command-line arguments** (parsed only in `RootStarter`s, `Scripts/Content/CmdArgs/`): `--server`, `--headless`, `--port <port>`, `--savefile <name>`, `--admin <uid>`, `--parent-pid <pid>`, `--no-hud`, `--world-render`, `--godot-log-push`, `--auto-start`, `--auto-connect`, `--auto-connect-ip <ip>`, `--auto-connect-port <port>`, `--nick <nick>`, `--uid <uid>`. Selection flag is `--server` (`DedicatedServerArgs.DedicatedServerFlag`, `DedicatedServerArgs.cs:15`).
- **Required env vars:** None directly read by game code. The README notes Godot's executable path is expected via ENV (set up by GodotUpdaterUI into `launchSettings.json`). `[ASK USER]` whether a `launchSettings.json` with those ENV vars is part of the project.
- **Deployment/runtime constraints:** Targets `net10.0`; supported OSes are Windows and Linux (`README.md:4`). The `NeonWarfare.csproj` `PreBuild` target pre-creates `bin/win-64` and `bin/android` dirs + a `bin/.gdignore` file (`NeonWarfare.csproj:17-25`).
- **Key engine settings** (`project.godot`):
  - Physics: **30 ticks/sec**, gravity disabled (`Vector2(0,0)`), linear damp 0, `physics_interpolation = true` (`project.godot:69-73`).
  - Locales: `en`, `ru` from `Assets/Locales/*.po` (`project.godot:65`).
  - Main scene: `uid://bjyux48ai45ry` → `Scenes/Root/Root.tscn` (`project.godot:18`).
  - Custom UI theme: `uid://cky2ixgrcrnlo` (`project.godot:28`).

### 6) Evidence

- `NeonWarfare.csproj` — dependency versions, target framework, SDK, PreBuild target.
- `project.godot` — engine config, physics, locales, main scene.
- `NeonWarfare.sln` — solution/project layout.
- `.editorconfig` — formatting rules.
- `README.md` — toolchain recommendations and run configurations.
- `docs/codebase/.codebase-scan.txt` — scan sections: STACK DETECTION, CI/CD, CONTAINERS, SECURITY, PERFORMANCE.
