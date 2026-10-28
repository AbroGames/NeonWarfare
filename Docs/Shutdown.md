# Shutdown

[← Project README](../README.md)

* **The client** — `Services.MainScene.Shutdown()` → a deferred `SceneTree.Quit()`.
* **A termination signal** — SIGTERM or SIGINT (Ctrl+C, `kill`, `systemctl stop`, `docker stop`). Godot
  installs no handler of its own, so the signal would kill the process on the spot, with no
  `NotificationExitTree` and hence no autosave. `TerminationSignalsService.Init()`, called
  from `BaseRootStarter.Init()` for both roles, cancels the signal through `PosixSignalRegistration` and
  calls `Services.MainScene.Shutdown()` instead. A second signal is not cancelled and kills the process
  — the way out when a shutdown hangs.
* **The server child process** — `ProcessShutdowner` (a node from GodotBox,
  `GodotBox.Godot.Nodes.Process`) is attached to `Game` in
  `HostDedicatedServerAndConnectGameStarter`. When the `Game` scene is destroyed, it kills the server
  process by the stored PID.
* **A server started from the client** — that same process was passed `--parent-pid`, and
  `DedicatedServerGameStarter` attaches a `ProcessDeadChecker` (also a node from GodotBox) to `Game`.
  It periodically checks whether the parent is alive and calls `MainScene.Shutdown()` if the client has
  disappeared from the OS.
* **Saving the world** — `WorldServerShutdowner` catches `NotificationExitTree` and calls
  `TryAutoSave()`. A separate node is needed because after leaving the tree `GetMultiplayer()` is
  already `null`, and by that moment `Network` might have swapped the peer for an
  `OfflineMultiplayerPeer` — they cannot be trusted.

The "client + out-of-process server" pair rests on two nodes at once and from two sides:
`ProcessShutdowner` kills the server on a normal client shutdown, and `ProcessDeadChecker` is the safety
net for the case where the client died abnormally and had no time to kill anyone.

Autosave is controlled by the `AutoSaveEnabled` setting: on the client — in `GameSettings`, on the
dedicated server — in `DedicatedServerSettings` (see [Data and saves](Data-and-saves.md)).
