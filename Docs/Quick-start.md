# Quick start

[← Project README](../README.md)

## Environment

* It is recommended to install and update Godot through
  [GodotUpdaterUI](https://github.com/AbroGames/GodotUpdaterUI/releases): this automatically sets up all
  the ENV variables used by the project in `launchSettings.json` — first of all `GODOT_EXE` (the path to
  the Godot executable).
* To set up the Rider integration in Godot, go to Editor → Editor Settings → Dotnet → Editor. In the
  External Editor list select JetBrains Rider and clear the Custom Exec Path Args value.

## Run profiles

For quick testing, run configurations are already set up in `Properties/launchSettings.json`. Rider
picks them up automatically. All the flags are described in [Command-line arguments](Cli-args.md).

| Profile | Arguments | What it does |
|---|---|---|
| `Client` | — | A normal client launch with the main menu |
| `Auto-start (new game)` | `--auto-start` | Straight into a single-player game with a new save file, skipping the menu |
| `Auto-start (saved game)` | `--auto-start --auto-start-savefile test` | The same, but with the `test` save: the first run creates it, later ones load it |
| `Server` | `--server --world-render` | A dedicated server with world rendering |
| `Autoconnect (1)` | `--auto-connect --uid TestPlayer1 --nick TestPlayer1` | A client with auto-connection |
| `Autoconnect (2)` | `--auto-connect --uid TestPlayer2 --nick TestPlayer2` | A second client with auto-connection |

## Multi-Launch: server and clients with one button

To bring up a server and one or two clients at the same time, Rider `Multi-Launch` configurations have
been added to the repository — the `.run/*.run.xml` files:

* Type: `Multi-Launch`. Name: `Fast-test (1 client)`. Tasks: `Server, Autoconnect (1)`.
* Type: `Multi-Launch`. Name: `Fast-test (2 clients)`. Tasks: `Server, Autoconnect (1), Autoconnect (2)`.

When editing `Properties/launchSettings.json` or `.run/`, this section must be edited in sync.
