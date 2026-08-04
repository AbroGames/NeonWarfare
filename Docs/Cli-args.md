# Command-line arguments

[← Project README](../README.md)

The arguments are described in `Scripts/Content/CmdArgs/` and are parsed **only** in the `RootStarter`s,
and from there passed on as ordinary parameters (see [Startup flow](Startup-flow.md)). Reading
`OS.GetCmdlineArgs()` from deep inside the code is not allowed.

The first one is Godot's standard `--path "./"` argument — it points at the project folder and has
nothing to do with the game's arguments.

**Common** (`CommonArgs`):

| Flag | Description |
|---|---|
| `--godot-log-push` | Mirror the Serilog logs into the Godot console |

**Client** (`ClientArgs`):

| Flag | Description |
|---|---|
| `--auto-start` | Immediately start a single-player game, skipping the menu (if `--auto-start-savefile` is not passed, then with a new save file) |
| `--auto-start-savefile <name>` | The save file name for `--auto-start`; if the file does not exist, a new game is created |
| `--auto-connect` | Immediately connect to a server, skipping the menu (the address and port are set by other flags) |
| `--auto-connect-ip <ip>` | The server address for auto-connection (if the flag is not passed, then `127.0.0.1`) |
| `--auto-connect-port <port>` | The port for auto-connection (if the flag is not passed, then `25566`) |
| `--nick <nick>` | Temporarily (without writing to the settings) override the nickname |
| `--uid <uid>` | Temporarily (without writing to the settings) override the player UID |

`--auto-start` and `--auto-connect` are mutually exclusive: if both are passed, `--auto-start` wins.

**Dedicated server** (`DedicatedServerArgs`):

| Flag | Description |
|---|---|
| `--server` | Run the process as a dedicated server (selects `DedicatedServerRootStarter`) |
| `--headless` | Run without a window |
| `--port <port>` | The port the server listens on (if the flag is not passed, then `25566`) |
| `--savefile <name>` | The save file name; if the file does not exist, a new game is created |
| `--admin <uid>` | The UID of the player who will be granted administrator rights |
| `--parent-pid <pid>` | The parent process PID; the server will shut down when the parent dies |
| `--no-hud` | Do not render the `ServerHud` |
| `--world-render` | Render the game world (by default the world is hidden and only the server console is visible) |

The server flags are assembled back into a command line by
`DedicatedServerArgs.GetArrayToStartDedicatedServer()` — this is exactly what the client uses to launch
an out-of-process server, passing it `--parent-pid` with its own PID.
