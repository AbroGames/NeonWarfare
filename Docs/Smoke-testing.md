# Smoke testing

[← Project README](../README.md)

`Tests/NeonWarfare.SmokeTests` launches the real game (`--headless`) and checks that it gets where it
was sent without complaining. It is the only automated check of the node tree — broken scenes, `null`
injections, exceptions in `_Ready`, failed connections; the unit tests of [Testing](Testing.md) cannot
see these.

## Running

```bash
dotnet test Tests/NeonWarfare.SmokeTests/NeonWarfare.SmokeTests.csproj
```

Needs `GODOT_EXE` (see [Quick start](Quick-start.md)). An assembly fixture builds the game project first, since
Godot runs already compiled assemblies, and runs `--import` once. Every scenario is a test class of its own, so
the scenarios run in parallel, and the suite takes about the build plus the longest scenario: about 20 s on 16
cores. Not run in CI, only compiled.

## Scenarios

| Test | Processes |
|---|---|
| `Client_StartsToMenu` | one client, no flags |
| `Client_StartsSingleplayerGame` | one client with `--auto-start` |
| `Server_AcceptsTwoClients` | `--server` plus two `--auto-connect` clients |
| `Client_QuitsFromMultiplayerGame` | an `--auto-host` client plus two `--auto-connect` clients, `client-1` quits first |
| `Server_LoadsItsAutosaveAfterRestart` | an `--auto-host-dedicated` client, whose child server autosaves; then `--server` loads that save, an `--auto-connect` client joins, and the server autosaves on `SIGTERM` |
| `Server_ShutsDownWhenParentDies` | `--server --parent-pid` of a stand-in process; the stand-in is killed, and the server must exit with its autosave |
| `Client_LoadsItsSingleplayerAutosaveAfterRestart` | two runs of `--auto-start --auto-start-savefile`, the second loads the save of the first |

`SmokeRun` flow:

1. Processes start one by one; each must print its milestones (`GameLaunch` — log line fragments proving
   it got where it was sent) before the next starts, so clients launch only after the server is up.
2. Everything lingers for 3 s to catch errors after a successful start.
3. Optional `Departure`: the leaver alone is stopped, a witness must print its milestone (the server:
   `Player left: <nick> (<uid>)`), the rest linger again. This is the only path where a client tears down a
   live world on exit and the server sees a player leave.
4. Optional `SelfExit`: its cause is run (killing the stand-in parent), and the exiter must exit on its own
   within 15 s with code 0.
5. The rest are stopped in launch order — so without a `Departure` the server always goes first and
   clients quit from the menu.
6. Each process must have printed its `ExitMilestones` (the autosave, say) by the time it is stopped.

A client hosting with `--auto-host-dedicated` logs `Started dedicated server process <pid>`. The child inherits
the client's stdout, so its lines are attributed to the client in the report.

Multiplayer scenarios use a free UDP port instead of the default `25566`, to avoid clashing with a
server started by hand.

## What counts as a failure

* a milestone missed (timeout, or the process exited first) — the report adds the process's last lines;
* a process exiting on its own while it should keep running;
* a child server still running once its client has exited (then killed). That the client waits for its child
  is checked by its exit milestone; this is the safety net;
* not exiting within 5 s of `SIGTERM` (then killed), or a non-zero exit code after it. While the game
  runs the exit code means nothing — `ExceptionHandlerService` swallows exceptions — so this is the only
  place it is checked;
* a Serilog line at `Warning`, `Error` or `Fatal`, with the exception text after it;
* an engine line starting with `ERROR:`, `WARNING:` or `SCRIPT ERROR:`, with its stack trace. This
  includes `ObjectDB instances were leaked at exit` — usually a node taken out of the tree or never added.

Identical problems (the Serilog timestamp aside) are reported once with a repeat count, and the report
is capped at about 200 lines: an exception on every physics frame would otherwise run to tens of thousands.

Stopping is `SIGTERM`, not a kill: the async Serilog sink is never flushed, so a kill would drop the lines
that explain a failure, and a graceful exit exercises [Shutdown](Shutdown.md) and autosave. Windows has
no `SIGTERM`: processes are killed and their exit is not checked (Windows not verified at all).

## Known gotchas

* Every process gets a fresh `user://` in the temp directory (via `XDG_DATA_HOME` / `APPDATA`; macOS uses
  the real one). So only first-run default settings are exercised. Runs sharing one `UserDataDirectories` give
  a process of the same name the same `user://` — that is how a restart scenario finds its save.
* A headless client is not a mode players use; window-dependent code may behave differently.
