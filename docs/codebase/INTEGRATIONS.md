# External Integrations

> **Note on scope:** NeonWarfare is a self-contained co-op game. It has **no third-party HTTP APIs, no cloud DB, no auth provider, and no telemetry/observability backend**. The "integrations" below are: the Godot high-level multiplayer transport, the local filesystem (`user://`), the OS process layer (dedicated-server IPC by PID), and the engine's translation server. There are no secrets or credentials in the codebase.

## Core Sections (Required)

### 1) Integration Inventory

| System | Type | Purpose | Auth model | Criticality | Evidence |
|--------|------|---------|------------|-------------|----------|
| Godot `SceneMultiplayer` + `ENetMultiplayerPeer` | Network transport (UDP/ENet) | Authoritative-server multiplayer: connect/host/open/shutdown, RPCs, spawning, field sync | None (trust-by-default; a `--admin <uid>` flag grants admin on the server) | **High** — core feature | `Network.cs:7,24-25,43-113`; `project.godot` (no port file) |
| Local filesystem (`user://`) | File store | Player settings, dedicated-server settings, resume-game record, binary world saves | OS user permissions | **High** — persistence | `SaveLoadService.cs:18-20,63-99` |
| OS process layer (`OS.CreateInstance` / `OS.GetProcessId`) | Process IPC (by PID) | `HostDedicatedServerAndConnectGameStarter` spawns a dedicated server as a **child process**; liveness checked by PID | None | Med — host-with-external-server mode | `ProcessService.cs:9-29`; `HostDedicatedServerAndConnectGameStarter.cs:17-26` |
| Godot `TranslationServer` | i18n | `Tr(KEY)` for all player-facing strings; locales `en`/`ru` | N/A | Med — UX | `project.godot:65`; `I18NService` (KludgeBox) delegates to `SceneTree.Tr` |
| NuGet (KludgeBox, CommunityToolkit.Mvvm, MessagePack) | Build-time packages | DI, source-gen models, binary serialization | N/A | High — foundational | `NeonWarfare.csproj:8-10` |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|--------------|----------|----------|
| `user://saves/<name>.bin` | Binary world saves (MessagePack blob of all `ISerializableStorage`) | `SaveLoadService` (disk I/O) + `WorldDataSerializerService` (serialize) + `WorldDataSaveLoadService` (RPC orchestration) | Single-file atomic write; corruption on crash mid-write; admin-only save enforced server-side | `SaveLoadService.cs:18,63-77`; `WorldDataSerializerService.cs:49-57` |
| `user://game-settings.json` | Client settings (`PlayerUid`, `PlayerNick`, `PlayerColor`, `Locale`, `AutoSaveEnabled`) | `GameSettingsService` via `System.Text.Json` (+ `ColorJsonConverter`) | None observed | `GameSettingsService.cs:9,47-68` |
| `user://dedicated-server-settings.json` | Dedicated-server settings (`Locale`, `AutoSaveEnabled`) | `DedicatedServerSettingsService` | None observed | `DedicatedServerSettingsService.cs:8` |
| `user://resume-game.json` | Last session for "Continue" button (`ResumableGame`) | `ResumableGameService` | None observed | `ResumableGameService.cs` |
| In-memory: `World.PersistenceData` / `TemporaryData` | Live world state (synced via RPC / `[Sync]`) | `World*Storage` nodes | Temporary data is **not** saved | `WorldPersistenceData.cs:15-16`; `WorldTemporaryData.cs:17` |

**Save file naming:** new save = `yyyy-MM-dd_HH-mm` (`SaveLoadService.cs:20`). Autosave is triggered by `WorldServerShutdowner` on `NotificationExitTree` and gated by `AutoSaveEnabled` (client `GameSettings` or server `DedicatedServerSettings`, `SaveLoadService.cs:29-34`).

### 3) Secrets and Credentials Handling

- **Credential sources:** None. No API keys, tokens, passwords, or connection strings exist in the codebase. Player identity is a locally generated random UID (`UidGenerator.cs:16-19` — 10 chars + "-" + 10 chars from `[A-Za-z]`) stored in `user://game-settings.json`.
- **Hardcoding checks:** Clean — scan found no secrets; no `.env.example`/`.env.template` exists (scan §ENVIRONMENT VARIABLE TEMPLATES: "No .env.example or .env.template found").
- **Admin model:** Server-side only. `--admin <uid>` grants a player admin on join (`DedicatedServerArgs.cs`; `WorldSynchronizerService` checks/grants). `IsAdmin(peerId)` returns true for `ServerId` or `playerData.IsAdmin` (`WorldFacadeService.cs:76-83`). Admin commands (`/admin`, `/surface`) are enforced in `WorldCommandService.ProcessCommand` (`WorldCommandService.cs:82-100`). There is **no password auth** — admin is granted by UID trust, which is acceptable for a LAN co-op game but not for open-internet hosting.

### 4) Reliability and Failure Behavior

- **Network reliability:**
  - Movement uses **unreliable** transfer with a monotonic `OrderId` for stale-packet dedup (`CharacterSynchronizer_Controller.cs:69-75`; `IController.MovementData.OrderId`). Beyond `DistanceForTeleport` (50px) the remote controller snaps instead of interpolating (`RemoteController.cs:10,49-51`).
  - Chat/stats/sync handshakes use **reliable** RPCs on separate `TransferChannel`s (`Chat`, `StatsHp`, `StatsCache`) so chatty streams don't block each other (`Consts.cs:15-20`; `WorldChatService.cs:28`).
  - `MaxSyncPacketSize = 135000` bytes (`Network.cs:10`).
- **Retry/backoff:** None for connection. `Network.ConnectToServer` fires one `ENetMultiplayerPeer.CreateClient`; on `ConnectionFailed`/`ServerDisconnected` it transitions to `Disconnected` and calls `Shutdown()` (no auto-reconnect) (`Network.cs:43-63,144-174`).
- **Timeout policy:** Not explicitly configured; relies on ENet defaults.
- **Circuit-breaker/fallback:** None. A failed client join returns to the menu with a message via `RejectSyncOnClientRpc(error)` (`WorldSynchronizerService.cs:71-90`).
- **Process-liveness fallback:** The dedicated-server-as-child relies on `ProcessDeadChecker` (server shuts down if parent PID dies) and `ProcessShutdowner` (client kills child server PID on Game exit) (`HostMultiplayerGameStarter.cs:27-34`; `HostDedicatedServerAndConnectGameStarter.cs:23-26`). Both are KludgeBox-provided.

### 5) Observability for Integrations

- **Logging around external calls:** Yes — Serilog `[Logger]` is pervasive. Connection host/port are logged as named fields (`README.md:477-479`); command discovery logs `"Added {count} chat commands."` (`WorldCommandService.cs:68`); duplicate commands warn.
- **Metrics/tracing coverage:** In-process only, via `WorldPerformanceService` and four sub-nodes rendered into the HUD:
  - `WorldGodotPerformance` — FPS, object/draw-call counts
  - `WorldSharpPerformance` — .NET memory
  - `WorldENetPerformance` — traffic, packet loss, per-peer RTT
  - `WorldPingPerformance` — custom ping mechanism (KludgeBox)
  
  (`WorldPerformanceService`; `Hud.cs:54-61` renders them every frame; `ServerHud.cs:56-72` shows per-player ping/loss). There is **no export to an external metrics backend**.
- **Debug affordances:** `Log` HUD button dumps the full node tree (`Services.NodeTree.LogFullTree(world)`, `Hud.cs:45`); `--godot-log-push` mirrors Serilog into the Godot editor console.
- **Missing visibility gaps:** No persistent log files configured (logs go to console only, unless KludgeBox's Serilog config writes elsewhere — `[TODO]`). No network packet-rate/bandwidth caps or alerts.

### 6) Evidence

- `Scenes/Game/Network/Network.cs:7,10,24-25,43-142` — transport lifecycle.
- `Scripts/Service/SaveLoadService.cs:14-20,29-34,63-99` — file persistence.
- `Scripts/Service/ProcessService.cs:9-29` — child-process server spawn.
- `Scripts/Service/Settings/UidGenerator.cs:16-19` — UID generation.
- `Scenes/World/Service/Command/WorldCommandService.cs:82-100` — admin enforcement.
- `Scenes/World/Service/Performance/` (4 perf nodes) — in-process metrics.
- `docs/codebase/.codebase-scan.txt` §ENVIRONMENT VARIABLE TEMPLATES, §SECURITY — "No .env", "No security configs detected".
