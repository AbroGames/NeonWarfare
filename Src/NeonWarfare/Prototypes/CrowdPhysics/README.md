# Crowd physics prototype

A throwaway benchmark that lets two crowd-physics variants be compared by numbers and by feel. The
decision it serves — what the real `Character` is built on — is made by a human running both branches;
this code never touches the real game.

Both branches carry a byte-identical harness (this folder plus the starter wiring) and differ only in
the physics variant:

| Branch | Variant |
|---|---|
| `proto/crowd-physics-characterbody` | A — `CharacterBody2D` |
| `proto/crowd-physics-rigidbody` | B — `RigidBody2D` |

## How to run

```bash
"$GODOT_EXE" --path "./" --physics-benchmark --bench-count 200    # interactive, 200 bots
"$GODOT_EXE" --path "./" --headless --physics-benchmark --bench-auto    # automatic CSV run
```

`--bench-count <n>` sets the initial bot count (default 100). `--bench-auto` runs the full sweep
without input (four configurations, bot count 50 → 500 in steps of 50, 2 s settle + 5 s measurement per
step) and quits after writing `user://physics-benchmark-<variant>-<timestamp>.csv` — the log prints the
globalized path. All flags are documented in `Docs/Cli-args.md`.

## Keys and mouse

| Input | Action |
|---|---|
| `1` / `2` / `3` | Steering mode: chase / wall / wander |
| `+` / `-` | Add / remove 50 bots |
| `B` | Player ↔ enemies block each other: on / off |
| `C` | Enemy ↔ enemy physical collisions (the baseline that reproduces the old jitter): on / off. Soft separation stays on either way |
| `P` | Projectiles on / off; `[` / `]` change the fire rate by 50/s |
| `H` | Hurtboxes on / off |
| `F` / `G` | Create / remove a runtime wall (at the cursor, along the player's facing) |
| RMB | Explosion knockback at the cursor |
| Mouse wheel | Zoom out / in |
| `R` | Reset: respawn all bots at random free points |
| WASD | Move the player (300 px/s) |

In `--bench-auto` mode all manual input is ignored.

## What is measured

The HUD (top left, twice a second) and the log (one line per second) report, over a one-second window:

- **TPS** — physics ticks actually executed per wall-clock second; below 60 means the game is slowing
  down. Also `Engine.MaxPhysicsStepsPerFrame` and how many frames hit the catch-up cap.
- **Physics tick time** — two numbers: what `Performance.Monitor.TimePhysicsProcess` reports
  (avg / p95 / max) and the harness's own wall time around the per-tick work (`callbacks`).
- **FPS**, the `Performance` physics monitors (`Physics2DActiveObjects`, `Physics2DCollisionPairs`,
  `Physics2DIslandCount`).
- **Jitter index** — for bots: the mean of `|v(t) − v(t−1)| / maxSpeed`, where `v` is the *actual*
  displacement per tick divided by the step; plus the share of bots whose actual movement direction
  flipped by more than 120° between consecutive ticks. A smooth crowd stays near 0.
- **outside** — bots beyond the arena bounds + margin: a cheap tunnelling / flung-across-the-map alarm.

## Harness layout

| File | What it is |
|---|---|
| `PhysicsBenchmarkRoot.cs` | The conductor: builds the tree, runs the per-tick pipeline, input, metrics |
| `IPhysicsVariant.cs` + `IBenchBody.cs` / `IBenchPlayerBody.cs` / `IBenchBotBody.cs` | The contracts a variant implements; the root discovers the single implementation in the assembly |
| `BotUnit.cs` | Harness-side per-bot state (intent, separation, knockback, wander target, previous velocity) |
| `BenchArena.cs` / `BenchWall.cs` | Static geometry with spawn-freeness checks; runtime walls |
| `BenchProjectile.cs` | Manually-moved `Area2D` projectiles |
| `SpatialHashGrid.cs` | The uniform grid behind the O(N) soft separation |
| `BenchmarkMetrics.cs` / `MetricsWindow.cs` | One-second metric windows |
| `BenchmarkHud.cs` | The overlay |
| `AutoBenchmark.cs` | The automatic sweep and the CSV |
| `BenchSpecs.cs` / `BenchLayers.cs` | Every tunable number and collision layer in one place |

## Variant A — `CharacterBody2D`

The player and the bots are `CharacterBody2D`; the prescribed velocity goes into `MoveAndSlide()`
every tick, inside the harness's `_PhysicsProcess`. Choices made and why:

- **`MotionMode = Floating`** — top-down, no "floor".
- **`MaxSlides`, `SafeMargin`, `PlatformOnLeave` stay at the engine defaults** (4 / 0.08 px / Additive).
  The arena has no platforms, and neither a corner squeeze nor a 900 px/s knockback produced a
  situation that needed more slides or a different margin in the automatic runs; when one shows up,
  these are the knobs to reach for.
- **Enemy-enemy and player-enemy blocking are pure layer/mask questions** (see `BenchLayers`): a bot's
  mask is `Walls (+ Enemies when C is on) (+ Player when B is on)`; the player's is
  `Walls (+ Enemies when B is on)`. The `B` toggle flips both sides, so blocking is symmetric.
- **Reported displacement includes the slides** — `LastDisplacement` is measured across the whole
  `MoveAndSlide()` call, which is what the jitter metric should see: the body's real per-tick motion.

### What the automatic run showed (headless, this machine)

From `physics-benchmark-characterbody-*.csv` written by `--bench-auto` (numbers are machine-specific;
the shape is the point):

- *chase + separation* holds TPS ≈ 60 up to 500 bots; the engine-side physics iteration (server step
  included) reaches ~15–17 ms at 450–500 bots, close to the 16.6 ms budget. The jitter index climbs to
  ~0.95 with ~79 % of bots flipping direction: the crowd oscillates on the pile point. This is soft
  separation doing its job at the cost of visible shimmer — the thing to compare against variant B by
  feel.
- *chase + self-collisions* (the baseline that reproduces the old jitter) collapses to **TPS 38.2,
  FPS 6.3 at 450 bots** in this run; a second run collapsed at 500 bots instead (35.7) — the exact
  breaking point depends on how the pile happens to form, which is its own kind of instability.
  Collision pairs ~1000–1170 at the collapse.
- *wall + separation* stays at TPS ≈ 60; the crowd presses into the long wall and shimmers less
  (jitter ~0.64, flips ~73 %).
- *projectiles + hurtboxes*: in chase mode the crowd sits on the player, so projectiles die within a
  fraction of a second and the alive count collapses to ~5 at 250+ bots (it is ~570 with 50 bots,
  before the pile forms). The "hundreds of live projectiles" cost is therefore better observed in the
  wall/wander modes interactively; the CSV records what actually lived.
- No bot ever escaped the arena (`outside 0` in every window): no tunnelling through walls at
  knockback speeds.

### `Performance.Monitor.TimePhysicsProcess` — what it includes

Checked in the Godot source (`main/main.cpp`, `Main::iteration()`; the value flows through
`performance->set_physics_process_time(...)`): the measured span starts before
`iteration_prepare()` and ends after `iteration_end()` of the same physics iteration — it covers the
interpolation prepare, both physics servers' `sync()`/`flush_queries()`, **all node
`_physics_process` callbacks**, the navigation server step, `PhysicsServer2D::step()` (the solver
itself) and the message-queue flush. So it is **not** callbacks-only: the server step is included.

Two caveats found in the same source: the monitor holds **seconds**, not milliseconds (this harness
multiplies by 1000), and it keeps the **running maximum** of physics-iteration times over the current
wall-clock second (reset once per second), so its "avg" over a window is an average of per-second
maxima and its p95/max nearly coincide. The honest per-tick distribution is the `callbacks` number;
the engine monitor's added value is that it includes the server step.
