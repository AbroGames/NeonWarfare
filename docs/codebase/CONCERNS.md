# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| **High** | **No automated tests** of any kind. The most fragile subsystem — client/server physics synchronization — is validated only manually. | `NeonWarfare.sln` (no test project); scan §CODE METRICS; `TESTING.md` | Regressions in movement/teleport/ramming physics ship undetected. The TODOs in `PhysicsCalculator.cs` show this area is actively unstable. | Add a `NeonWarfare.Tests` project; unit-test node-free logic (`PhysicsCalculator.CalculateAnalyticMotion`, `StatusEffect` builders, `ControlBlockerHandler`, serializer round-trips, command parsing). |
| **High** | **Client/server physics synchronization has multiple unresolved, documented defects.** Player passes through enemies at high speed; `Mass` no longer affects ramming; server TPS load 33–70% for 300 static units vs 14% on client; spawn teleport causes a one-frame flash to (0;0). | `PhysicsCalculator.cs:9-35,62-77`; `RemoteController.cs:42-44`; `WorldCharacterService.cs:32-38`; `Character.cs:44-46` | Core gameplay feel and fairness break with many units / at speed; netcode correctness in question. | Profile both sides, decide client/server physics split, fix the spawn-position flow (pass coords at spawn via RPC or `MpSpawner.SpawnFunction`). |
| **Med** | **Admin granted by UID trust, no auth.** Anyone who knows/sets a UID with `--admin` (or an existing admin via `/admin add`) gets full control. | `DedicatedServerArgs.cs`; `WorldFacadeService.cs:76-83`; `WorldCommandService.cs:82-100` | Fine for LAN co-op; unsafe if the dedicated server is exposed on the open internet (kick/ban, surface switch, save control). | Document the threat model; add a password/token gate before exposing servers publicly. `[ASK USER]` is public-internet hosting an intended use case? |
| **Med** | **Dedicated-server lifecycle depends on PID-based IPC** (`ProcessDeadChecker`/`ProcessShutdowner`). Process-tree semantics differ across OSes (notably Linux process groups vs Windows job objects). | `HostMultiplayerServerGameStarter.cs:27-34`; `HostDedicatedServerAndConnectGameStarter.cs:23-26` | Orphaned server processes if the PID check misfires or the parent dies uncleanly. | Add a startup log of the child PID + a fallback heartbeat/timeout; verify behavior on both Windows and Linux. |
| **Med** | **README states versions as "latest"; code pins Godot 4.7 + .NET 10 (`net10.0`).** Drift between docs and build can break contributor setup silently. | `README.md:33-46` vs `project.godot:19`, `NeonWarfare.csproj:1,3` | Contributors following the README may install a different engine/runtime and hit obscure errors. | Update README to pin `Godot 4.7 (.NET)` + `net10.0`. |
| **Low** | **`BattleSurface` vs `SafeSurface` reality contradicts the README.** README §"Текущее состояние" claims they "don't yet differ in logic", but `SafeSurface.InitOnServer()` spawns 3 walls + 9 test bots while `BattleSurface` is an empty stub. | `SafeSurface.cs:9-35`; `BattleSurface.cs` (empty `{ }`); `README.md:523` | Misleading docs; the "safe" surface is really a demo/testbed. | Either fix the README, or split the demo content out of `SafeSurface` so it matches its name. `[ASK USER]` what the intended Safe/Battle distinction is. |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|---------------|-------|-----------------|---------------|
| **`Test1/2/3` debug buttons + `World.Test1/2/3` RPCs in production code** | Ad-hoc testing without a test framework | `Hud.cs:42-44`, `ServerHud.cs`, `World.cs:98-144` | Ship debug RPCs that spawn arbitrary enemies; clutter | Remove once a test harness or a real spawn command exists (`README.md:514` flags these `//TODO`). |
| **`NavigationService` + `Pathfinder` are dead code** | Written for a navigation feature that was never wired into the world | `Scenes/World/Service/NavigationService.cs`, `Scenes/Entity/Characters/Controller/Ai/Pathfinder.cs` (zero external references; `.tscn` files contain no `NavigationAgent2D`) | Maintenance burden; readers assume pathfinding works | Either wire it into `World`/AI or delete it. `NavigationService.cs:171` self-documents as "useless junk for now". `[ASK USER]` is pathfinding planned soon? |
| **`ClientPackedScenes` is an empty storage** | Reserved for client-only visual scenes that don't exist yet | `Scenes/World/Scenes/ClientScenes/ClientPackedScenes.cs` (empty class body) | Dead node in the World tree | Keep as a placeholder or remove until needed. |
| **Legacy `ContextStorage`** | Superseded by `PagesProvider` page-stack menu | `Scenes/Screen/NewMenu/ContextStorage.cs` (exports unused `MainContext/SettingsContext/ConnectionContext`) | Confusion about which menu system is live | Delete; `PagesProvider` is the active system (`PagesProvider.cs:8-16`). |
| **`CharacterController` complexity / "split into two classes?" open question** | Single class serves both server and client data sources via `syncToClient` flags | `CharacterController.cs:12,22,65-66`; README notes possible Facade/server-client split | Hard to reason about; sync-loop bugs | Resolve the design TODO: keep unified with flags, or split into server/client controllers + a Facade. |
| **`PhysicsCalculator` tuning hardcoded** | `_force=5000`, `GroundFriction=2000`, `AirFriction=0.04`, `MaxSpeed=1000` as readonly fields; `MaxSpeed` clamp commented out | `PhysicsCalculator.cs:18-21,56-58` | Can't balance without recompile; dead clamp hides overspeed | Make data-driven (stats/config); restore or remove the clamp deliberately. |
| **5 stale `.csproj.old*` backups in repo root** | Manual version snapshots of the project file | `NeonWarfare.csproj.old`, `.old.1`…`.old.4` | Repo noise; risk of editing the wrong file | Rely on git history; delete the `.old*` files. |
| **`World.cs:98-144` test methods with TODO** | Temp spawn test code left in the authoritative World node | `World.cs:98` | Pollutes the service container's core node | See "Test1/2/3" row above. |
| **Duplicate clamp logic between `CharacterStats` and `CharacterStatsClient`** | Server/client stat mirrors reimplement the same clamps | `CharacterStats.cs:121-135` vs `CharacterStatsClient.cs:77-91` (TODO at `:56,94`) | Divergence risk | Extract a shared stat-clamp helper. |

### 3) Security Concerns

| Risk | OWASP category | Evidence | Current mitigation | Gap |
|------|----------------|----------|--------------------|-----|
| No authentication / admin-by-UID | A07 (Identification & Auth failures) | `WorldFacadeService.cs:76-83`; `--admin` flag | Acceptable for trusted LAN; admin gated server-side | No mitigation if server is internet-exposed (see `[ASK USER]`) |
| No input validation on **player color** beyond luminance ≥ 0.2; nick only length 3–25 + no spaces | A03 (Injection) — low relevance (no SQL/shell) | `WorldSynchronizerService.cs:71-90` | Basic format checks at join | `[TODO]` — confirm MessagePack payloads from peers are size/bounds-checked; `MaxSyncPacketSize=135000` (`Network.cs:10`) caps sync packets |
| No transport encryption | A02 (Cryptographic failures) | ENet plaintext UDP (`Network.cs:24-25`) | None | Acceptable for LAN; not for public hosting |
| Saves/settings are unencrypted local files | N/A (local trust) | `user://*.json`, `*.bin` | OS user perms | None — by design for a local game |
| No SECURITY.md / dependency scanning / SBOM | N/A | scan §SECURITY: "No security configs detected" | None | Add a `SECURITY.md` threat model if the project will be publicly hosted |

> No hardcoded secrets, API keys, or tokens were found in the scan or source. Player identity is a locally random UID (`UidGenerator.cs:16-19`).

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|-----------------|--------------|-----------------------|
| **Server TPS cost grows non-linearly with unit count** | Author's own profiling notes: 300 units → server 33% (spikes 70%) vs client 14% TPS; 10 units ≈ 4% both sides | High server CPU at scale | Caps concurrent units/players | Investigate why static units cost more on the server (`PhysicsCalculator.cs:25-35`); profile `_IntegrateForces` per unit. |
| **`RemoteController` extrapolation can teleport** beyond 50px | `DistanceForTeleport = 50`, `InertiaTime = 0.2` | Visible snapping under lag/packet loss | Worse with more peers/loss | Switch extrapolation → interpolation as the author notes (`RemoteController.cs:42`); tune per-net-condition. |
| **Movement RPC is per-unit, unreliable** | `Controller_SendMovement` sends one `MovementData` per unit per frame (`CharacterSynchronizer_Controller.cs:69-75`) | Bandwidth scales with units×tickrate | 30 Hz × N units × peers | Batch multiple units' positions into one RPC (README convention already recommends batching). |
| **`ObservableProperty` writes generate network traffic** | Every server-side model set fires `PropertyChanged` → RPC broadcast | Unintended writes = unintended traffic | Hot paths (frequent stat changes) amplify | Batch/throttle high-frequency property changes; document hot properties. |
| **Large binary assets committed** (`.psd` up to 8.6 MB; `.png` 0.5–0.6 MB) | scan §CODE METRICS top files: `Assets/Textures/UI/Icons/Achievements/Ninja.psd` (8.6 MB), `Crowd.psd` (1.7 MB) | Bloats repo | Slow clones | Move source `.psd` out of the repo; keep only exported `.png`/`.ctex`. |
| **No bandwidth/traffic caps or alerts** | `WorldENetPerformance` only *displays* metrics | No automatic throttling | Runaway traffic unnoticed | Add thresholds/warnings in the perf service. |

### 5) Fragile / High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|------|-------------|--------------|----------------------|
| `Scenes/Entity/Characters/Controller/Player/PhysicsCalculator.cs` | Analytic force/friction/air-drag solver; client and server must agree exactly; many open design TODOs | 22+ TODO comments in one file (scan §TODO) | Change physics constants on both sides together; add unit tests for `CalculateAnalyticMotion` first |
| `Scenes/Entity/Characters/Character.cs` + `CharacterSynchronizer_*.cs` | Spawn/controller-init ordering is subtle (default `RemoteController`, teleport on spawn, interpolation disabled 1 frame) | Dense TODOs (`Character.cs:28-46`; `WorldCharacterService.cs:32-38`) | Touch the spawn path last; reproduce the (0;0) flash in a minimal scene before refactoring |
| `Scenes/Game/Network/Network.cs` shutdown path | Order-dependent: by `NotificationExitTree` the peer may already be `OfflineMultiplayerPeer` | `WorldServerShutdowner` exists specifically to work around this (`WorldServerShutdowner.cs:9-13`) | Never trust `GetMultiplayer()`/`Network` in `_exit_tree`-adjacent code; route through the shutdowner |
| `Scenes/World/Service/WorldSynchronizerService.cs` | Join handshake is the trust boundary (validation + world snapshot) | Single RPC chain with reject/accept branches | Add tests for each reject path before changing validation rules |
| Project files (`NeonWarfare.csproj`) | 5 `.old*` backups indicate recent churn/breakage | `NeonWarfare.csproj.old{,.1..4}` in root | Edit only `NeonWarfare.csproj`; delete backups; rely on git |

> **Note:** scan §GIT RECENT COMMITS and §HIGH-CHURN FILES returned "No commits found" / "None found" — the working copy is **not a git repo** in this scan's view (despite a `.git/` directory existing). Git-history-based churn analysis was not possible. `[ASK USER]` whether the project is version-controlled elsewhere; the fragility assessments above are based on TODO density and architectural sensitivity instead.

### 6) `[ASK USER]` Questions

1. **[ASK USER]** Is **public-internet hosting** of the dedicated server an intended use case? (Determines whether UID-trust admin and plaintext ENet are acceptable or need auth/encryption — Concerns §1, §3.)
2. **[ASK USER]** What is the **intended gameplay distinction between `SafeSurface` and `BattleSurface`**? The README says they don't differ yet, but `SafeSurface` already spawns demo walls/bots while `BattleSurface` is empty. Should the demo content move out of `SafeSurface`? (§1 row 6.)
3. **[ASK USER]** Is **pathfinding** (`NavigationService` + `Pathfinder`) planned for the near term? If not, should the dead code be deleted? (§2.)
4. **[ASK USER]** Is there a **`launchSettings.json`** (with Godot-path ENV vars set by GodotUpdaterUI) that should be part of this project/docs? It's referenced by the README but not seen in the scan. (STACK.md §5.)
5. **[ASK USER]** Is this project **version-controlled** (git)? The scan found no commits despite a `.git/` dir. Where does history live, so churn/fragility can be measured properly? (§5.)
6. **[ASK USER]** The README and the code **diverge on versions** ("latest" vs pinned Godot 4.7 / `net10.0`). Which is the source of truth to document? (§1 row 5.)

### 7) Evidence

- `docs/codebase/.codebase-scan.txt` — §TODO/FIXME (production), §CODE METRICS (largest assets, file/language counts), §GIT/HIGH-CHURN, §SECURITY/PERFORMANCE/CI-CD ("none detected").
- `Scenes/Entity/Characters/Controller/Player/PhysicsCalculator.cs:9-35,62-77,18-21` — physics debt + tuning.
- `Scenes/Entity/Characters/Controller/Remote/RemoteController.cs:10,42-44,49-51` — remote-controller fragility.
- `Scenes/World/Service/WorldSynchronizerService.cs:71-90` — join validation.
- `Scenes/World/Service/StartStop/WorldServerShutdowner.cs:9-13` — shutdown-ordering hazard.
- `Scenes/Game/Starters/HostMultiplayerGameStarter.cs:27-34`, `HostDedicatedServerAndConnectGameStarter.cs:23-26` — PID IPC.
- `README.md:517-527` — authored "current state" (cross-checked; surfaces the Safe/Battle divergence and unused pathfinding).
