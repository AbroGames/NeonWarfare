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

## Variant

See the variant section of the branch commits: this commit intentionally contains no
`IPhysicsVariant` implementation, and the benchmark exits with an error when run without one.
