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

Every process runs with `--headless`; a run takes about ten seconds per scenario.

| Test | Processes |
|---|---|
| `Client_StartsToMenu` | one client, no flags |
| `Client_StartsSingleplayerGame` | one client with `--auto-start` |
| `Server_AcceptsTwoClients` | `--server` plus two `--auto-connect` clients |

The multiplayer scenario takes a free port from the dynamic range instead of the default `25566`, so
it does not collide with a server started by hand.

## What counts as a failure

The exit code says nothing — `ExceptionHandlerService` catches unhandled exceptions, logs them and
lets the process live on. So the output is scanned instead, and anything below fails the test:

* a Serilog line at `Warning`, `Error` or `Fatal` — the level is rendered as a padded full name, as in
  `|09:40:22.851| (      Error) (...)`;
* an engine line starting with `ERROR:`, `WARNING:` or `SCRIPT ERROR:`, together with its stack trace.

Output is captured from each process rather than read from `user://logs/godot.log`: a scenario runs
several instances of the same project, and they rotate that one file out from under each other.

Processes are stopped with `SIGTERM` rather than killed. The Serilog sink is asynchronous and nothing
flushes it on exit, so a hard kill would drop the very lines that explain a failure; a graceful exit
also runs the autosave path from [Shutdown](Shutdown.md).

## Not in CI

The GitHub runner has no engine, so the `Test` step names the unit test project explicitly. The smoke
project is still compiled there — only never run.

## Known gotchas

* The tests use the real `user://`, so local settings and saves affect the result. `--auto-start`
  leaves a new save file behind on every run.
* All processes of a scenario start at once. `HostMultiplayerGameStarter` opens the socket only after
  the world exists and the client does not retry, so a client can lose the race. If that turns out to
  be flaky, wait for `Started server successfully` on the server's output before launching clients.
* Running a client headless is not a mode players use; parts of the code that depend on a window may
  behave differently there.
