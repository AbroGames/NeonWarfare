# Testing Patterns

## Core Sections (Required)

### 1) Test Stack and Commands

- **Primary test framework:** **None.** There is no automated test project, no test framework (`xUnit`/`NUnit`/`Microsoft.NET.Test.Sdk`), and no `[Test]`/`[Fact]`/`[Theory]` attributes anywhere in the scan.
- **Assertion/mocking tools:** None configured.
- **Commands:**

```bash
# (none — there is no `dotnet test` target)
# The only validation is manual: run the game and exercise features.
dotnet build NeonWarfare.sln         # compile (this is the only build/test command)
```

The scan's §TODO/FIXME section was run with test directories excluded and found only production TODOs. No `tests/`, `test/`, `__tests__/`, or `*.Tests` project exists (scan §CODE METRICS: C# files = 147, none under a test path).

### 2) Test Layout

- **Test file placement pattern:** **No tests exist.** No co-located test files, no dedicated tests folder, no test project in the `.sln`.
- **Naming convention:** N/A.
- **Setup files:** None.
- **What exists instead — manual/in-game debugging:**
  - `Hud` (client) and `ServerHud` expose **`Test1` / `Test2` / `Test3` buttons** that spawn bots/enemies via `World.Test1()/Test2()/Test3()` RPCs (`Hud.cs:42-44`; `World.cs:98` — the methods are explicitly marked `//TODO Test methods. Remove after tests.`).
  - A **`Log`** HUD button prints the entire node tree (`Services.NodeTree.LogFullTree(world)`, `Hud.cs:45`).
  - **Performance HUD** (`WorldPerformanceService` + 4 sub-nodes) renders Godot/.NET/ENet/Ping metrics live (`Hud.cs:54-61`, `ServerHud.cs:56-72`).
  - README's "Fast-test" Rider multi-launch configs spin up `Server` + 1 or 2 `--auto-connect` clients for manual multiplayer testing (`README.md:59-65`).

### 3) Test Scope Matrix

| Scope | Covered? | Typical target | Notes |
|-------|----------|----------------|-------|
| Unit | **No** | — | No unit tests anywhere |
| Integration | **No** | — | No integration tests; multiplayer is tested manually via run configs |
| E2E | **Manual only** | Game flows (singleplayer/connect/host) | Manual: launch combinations of client + dedicated server; exercise HUD Test buttons |

### 4) Mocking and Isolation Strategy

- **Main mocking approach:** None — there is no test harness.
- **Isolation guarantees:** N/A. In-process, the design itself isolates server/client halves via `Net.DoServerClient`/`DoServerNotServer`, but this is a runtime role split, not a test seam (`Character.cs:35-46`).
- **Common failure mode in tests:** N/A.

### 5) Coverage and Quality Signals

- **Coverage tool + threshold:** None.
- **Current reported coverage:** N/A.
- **Known gaps/flaky areas:**
  - The most fragile area — **client/server physics sync** — has extensive inline TODOs documenting unresolved questions (spawn teleport, `ForceCoef`, `Mass` not working on ramming, high server TPS load at 10–300 units) (`PhysicsCalculator.cs:9-35,62-77`; `WorldCharacterService.cs:32-38`; `RemoteController.cs:42-44`). These are validated **only manually** today.
  - The `Test1/2/3` HUD buttons and `World.Test1/2/3` RPCs are flagged for removal (`World.cs:98`).
  - **Recommendation:** introducing a test project (e.g. `NeonWarfare.Tests`) targeting the pure, node-free logic (`PhysicsCalculator.CalculateAnalyticMotion`, `StatusEffect` builders, `ControlBlockerHandler`, `WorldDataSerializerService` round-trips, command parsing) would cover the most error-prone, framework-light code first.

### 6) Evidence

- `docs/codebase/.codebase-scan.txt` §CODE METRICS — 147 C# files, none in a test path.
- `docs/codebase/.codebase-scan.txt` §TODO/FIXME — production-only TODOs (test dirs excluded).
- `NeonWarfare.sln` — single project, no `*.Tests` project referenced.
- `Scenes/Screen/Hud/Hud.cs:42-45` — manual Test/Log buttons.
- `Scenes/World/World.cs:98` — `//TODO Test methods. Remove after tests.`
- `README.md:59-65, 508-516` — manual run configs and debug/perf tooling.
