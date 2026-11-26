# Shutdown

[← Project README](../README.md)

* **The client** — `Services.MainScene.Shutdown()`: frees the `Game`, waits for the child server (below), then
  `SceneTree.Quit()`. Closing the window goes the same way: `QuitRequestsService` turns `AutoAcceptQuit` off and
  calls `Shutdown()` on the root window's `CloseRequested`.
* **A termination signal** — SIGTERM or SIGINT (Ctrl+C, `kill`, `systemctl stop`, `docker stop`). Godot
  installs no handler of its own, so the signal would kill the process on the spot, with no
  `NotificationExitTree` and hence no autosave. `QuitRequestsService.Init()`, called
  from `BaseRootStarter.Init()` for both roles, cancels the signal through `PosixSignalRegistration` and
  calls `Services.MainScene.Shutdown()` instead. A second signal is not cancelled and kills the process
  — the way out when a shutdown hangs.
* **The server child process** — `OS.Kill` is SIGKILL / `TerminateProcess`, with no autosave, so the client never
  asks the server to stop: freeing the `Game` closes the connection, the admin leaves, and `PlayerSimulationFacade`
  reports it through `IServerOwner.AdminLeft()`, which `Game` raises as its `AdminLeft` event. The admin displaced by
  its own new connection has not left, and the server keeps running.
  `DedicatedServerGameStarter` started with `--parent-pid` subscribes to it and calls `MainScene.Shutdown()`; one
  started from the console keeps running. On the client
  `MainScene.StartMainMenu()` and `Shutdown()` go on only through `Services.Process.WaitForDedicatedServerExit`:
  the "Stopping the server" loading screen until the process exits, `OS.Kill` after 10 s.
* **A server started from the client** — that same process was passed `--parent-pid`, and
  `DedicatedServerGameStarter` attaches a `ProcessDeadChecker` (also a node from GodotBox) to `Game`.
  It periodically checks whether the parent is alive and calls `MainScene.Shutdown()` if the client has
  disappeared from the OS.
* **Saving the world** — `SaveOnExitNode` calls `SaveService.SaveOnExit` when the World leaves the tree,
  see [Data and saves](Data-and-saves.md#saves).

The "client + out-of-process server" pair is stopped from the server side both ways: by the admin leaving on a
normal client shutdown, and by `ProcessDeadChecker` when the client died abnormally — a crashing client kills
nothing, so the server still autosaves.

Autosave is controlled by the `AutoSaveEnabled` setting: on the client — in `GameSettings`, on the
dedicated server — in `DedicatedServerSettings` (see [Data and saves](Data-and-saves.md)).
