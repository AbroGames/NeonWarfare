# Smoke testing

[← Project README](../README.md)

`Tests/NeonWarfare.SmokeTests` launches the real game and checks that it starts without complaining.
This is the only automated check of what actually happens inside the node tree — a broken scene, an
injection that comes out `null`, an exception in `_Ready`, a client that never reaches the server. The
unit tests of [Testing](Testing.md) cannot see any of that.

## Running

```bash
dotnet test Tests/NeonWarfare.SmokeTests/NeonWarfare.SmokeTests.csproj
```

Needs `GODOT_EXE` — the same variable the launch profiles use, see [Quick start](Quick-start.md). A
fixture builds the game project first, because Godot runs assemblies that are already compiled.

## Scenarios

Every process runs with `--headless`; the whole suite takes about fifteen seconds.

| Test | Processes |
|---|---|
| `Client_StartsToMenu` | one client, no flags |
| `Client_StartsSingleplayerGame` | one client with `--auto-start` |
| `Server_AcceptsTwoClients` | `--server` plus two `--auto-connect` clients |

Silence is not success, so every process also has milestones — fragments of its own log lines that
prove it got where it was sent (`GameLaunch`):

| Process | Milestones |
|---|---|
| menu client | `Starting Client...` |
| `--auto-start` client | `Syncing complete successfully` |
| server | `Started server successfully` |
| `--auto-connect` client | `Connected to the server successfully`, then `Syncing complete successfully` |

Processes start one by one, each after the previous has printed its milestones, so a client is only
launched once the server's socket is open. When every milestone is in, all processes keep running for
three more seconds to catch an error that follows a successful start.

The multiplayer scenario takes a free UDP port (ENet is UDP) from the dynamic range instead of the
default `25566`, so it does not collide with a server started by hand.

## What counts as a failure

The exit code says nothing — `ExceptionHandlerService` catches unhandled exceptions, logs them and
lets the process live on. So the output is watched instead, and anything below fails the test:

* a milestone not printed within 60 seconds, or the process exiting before printing it — the report
  then carries the last lines of that process's output;
* a process exiting on its own after its milestones, while it should have kept running;
* a Serilog line at `Warning`, `Error` or `Fatal` — the level is rendered as a padded full name, as in
  `|09:40:22.851| (      Error) (...)` — together with the exception text printed after it;
* an engine line starting with `ERROR:`, `WARNING:` or `SCRIPT ERROR:`, together with its stack trace.

Output is captured from each process rather than read from `user://logs/godot.log`: milestones are
awaited as the lines arrive, and ANSI escapes, which `GD.PrintRich` emits even into a pipe, are
stripped on the way in.

Every process gets its own fresh `user://` in the temp directory, deleted afterwards, so the
developer's settings and saves neither affect a run nor collect its leftovers. Godot derives
`user://` from `XDG_DATA_HOME` on Linux and from `APPDATA` on Windows (the latter not verified); on
macOS it hangs off `HOME`, which is too broad to override, so there the real `user://` is still used.

Processes are stopped with `SIGTERM` rather than killed. The Serilog sink is asynchronous and nothing
flushes it on exit, so a hard kill would drop the very lines that explain a failure; a graceful exit
also runs the autosave path from [Shutdown](Shutdown.md).

## Not in CI

The GitHub runner has no engine, so the `Test` step names the unit test project explicitly. The smoke
project is still compiled there — only never run.

## Known gotchas

* A fresh `user://` means first-run defaults: whatever a developer changed in their own settings is
  not exercised here.
* Running a client headless is not a mode players use; parts of the code that depend on a window may
  behave differently there.
