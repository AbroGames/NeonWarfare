# Command-line arguments

[← Project README](../README.md)

The arguments are described in `Src/NeonWarfare/Scripts/Content/CmdArgs/` and are parsed **only** in the
`RootStarter`s, and from there passed on as ordinary parameters (see [Startup flow](Startup-flow.md)). Reading
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

**Physics benchmark** (`PhysicsBenchmarkArgs`):

| Flag | Description |
|---|---|
| `--physics-benchmark` | Run the crowd physics prototype instead of the game: an arena where bots crowd under different steering modes, built to compare `CharacterBody2D` and `RigidBody2D` crowds by TPS, physics tick time and jitter (selects `PhysicsBenchmarkRootStarter`) |
| `--bench-count <n>` | The initial bot count of the benchmark (if the flag is not passed, then `100`) |
| `--bench-auto` | Run the automatic benchmark: the `legacy` suite (four configurations, the bot count ramped 50 → 500, one CSV row per step written to `user://`), then quit (works together with `--headless`) |
| `--bench-suite <name>` | Which suite to run: `sanity`, `legacy`, `core`, `blocking`, `load`, `load-rate`, `events` or `realtime` (the configurations are listed in the prototype `README.md`). Implies `--bench-auto` |
| `--bench-out <dir>` | Absolute directory for the benchmark CSV (if the flag is not passed, then `user://`) |
| `--bench-rep <n>` | The repetition number (if the flag is not passed, then `1`); goes into the CSV and the file name, and offsets the random seed (`seed = RandomSeed + n`), so repetitions differ in pile formation, not just in timing noise |
| `--bench-throughput` | Throughput mode: metric windows close every 60 physics ticks instead of every wall-clock second. Run it together with Godot's `--fixed-fps 60` (the `run-suites.sh` script passes both), so the wall time per frame is the full cost of one tick |
| `--bench-ccd` | Turn continuous collision detection on for the benchmark bodies (a real switch for `RigidBody2D`, a no-op for `CharacterBody2D`) |

**Dedicated server** (`DedicatedServerArgs`):

| Flag | Description |
|---|---|
| `--server` | Run the process as a dedicated server (selects `DedicatedServerRootStarter`) |
| `--headless` | Run without a window |
| `--port <port>` | The port the server listens on (if the flag is not passed, then `25566`) |
| `--savefile <name>` | The save file name; if the file does not exist, a new game is created |
| `--admin <uid>` | The UID of the player who will be granted administrator rights |
| `--parent-pid <pid>` | The parent process PID; the server will shut down when the parent dies |

The server flags are assembled back into a command line by
`DedicatedServerArgs.GetArrayToStartDedicatedServer()` — this is exactly what the client uses to launch
an out-of-process server, passing it `--parent-pid` with its own PID.
