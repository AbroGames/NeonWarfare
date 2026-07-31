# Shutdown

[← Project README](../README.md)

* **The client** — `Services.MainScene.Shutdown()` → a deferred `SceneTree.Quit()`.
* **The server child process** — `ProcessShutdowner` (a node from KludgeBox,
  `KludgeBox.Godot.Nodes.Process`) is attached to `Game` in
  `HostDedicatedServerAndConnectGameStarter`. When the `Game` scene is destroyed, it kills the server
  process by the stored PID.
* **A server started from the client** — that same process was passed `--parent-pid`, and
  `HostMultiplayerGameStarter` attaches a `ProcessDeadChecker` (also a node from KludgeBox) to `Game`.
  It periodically checks whether the parent is alive and calls `MainScene.Shutdown()` if the client
  has disappeared from the OS.
* **Saving the world** — `WorldServerShutdowner` catches `NotificationExitTree` and calls
  `TryAutoSave()`. A separate node is needed because after leaving the tree `GetMultiplayer()` is
  already `null`, and by that moment `Network` might have swapped the peer for an
  `OfflineMultiplayerPeer` — they cannot be trusted.

The "client + out-of-process server" pair rests on two nodes at once and from two sides:
`ProcessShutdowner` kills the server on a normal client shutdown, and `ProcessDeadChecker` is the
safety net for the case where the client died abnormally and had no time to kill anyone.

Autosave is controlled by the `AutoSaveEnabled` setting: on the client — in `GameSettings`, on the
dedicated server — in `DedicatedServerSettings` (see [Data and saves](Data-and-saves.md)).
