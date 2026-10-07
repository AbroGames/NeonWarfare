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

## Variant B — `RigidBody2D`

The player and the bots are `RigidBody2D` (`GravityScale = 0`, `LockRotation = true`, `LinearDamp = 0`,
`CanSleep = false`). Choices made and why:

- **`_IntegrateForces`, not `_PhysicsProcess`.** The integrate callback is the engine-sanctioned place
  to mutate body state: the velocity written there is used by the solver within the same step, the
  post-solve velocity of the previous step is readable there reliably, and per-body ordering cannot
  race the harness's `_PhysicsProcess`. `_PhysicsProcess` would write the same property one callback
  earlier, with no benefit.
- **The solver's collision response is never discarded.** Writing a raw velocity every tick is exactly
  what broke rams and caused tunnelling in the old `PhysicsCalculator`. Instead, at integrate time the
  body measures what the solver did to the last prescription (`state.LinearVelocity − lastDesired`),
  clamps it to 400 px/s, halves it (a bump fades instead of ringing) and adds it back on top of the
  fresh prescription. Rams still push, wall hits do not accumulate into launches.
- **CCD off.** Knockback peaks at 900 px/s = 15 px per tick against 30–40 px walls; no bot ever left
  the arena in the automatic runs (`outside 0` everywhere). If a faster knockback ever tunnels, flip
  `UseContinuousCollision` in `RigidBodyBenchBody` to `CastShape` — deliberately a one-line constant,
  not a toggle key, so the measured configuration stays fixed.
- **`CanSleep = false`** — a sleeping body would ignore the soft separation pushes.
- **Teleports (`PlaceAt`) go through the next `_IntegrateForces`** as `state.Transform` write + zeroed
  velocity + `ResetPhysicsInterpolation()`: the one-tick delay is invisible for a respawn, and writing
  a rigid body transform outside integration is not reliable.
- **Reported displacement is start-to-start** between consecutive integrates — it includes the
  solver's positional correction, one tick later than variant A measures; consistent within the
  variant, which is what the comparison needs.
- The trade of the retained response: the solver partly owns the movement. Near walls the body keeps
  a small decaying press into the contact, and knockback trajectories are less exactly scripted than
  in variant A. Whether that *feels* right is exactly what the interactive run is for.

### What the automatic run showed (headless, this machine)

From `physics-benchmark-rigidbody-*.csv` written by `--bench-auto`, same harness and seed as the
`CharacterBody2D` branch (numbers are machine-specific; the shape is the point):

- *chase + self-collisions* — the headline. Variant A (kinematic bots colliding) collapses to
  36–38 TPS at 450–500 bots; here the solver resolves the crowd and **TPS stays 60 at every count up
  to 500**, with a jitter index of 0.01–0.03 and direction flips under 2.5 % (A: ~0.3 and ~90 % at
  the same counts). Overlap resolution by the solver is simply not the same workload as every body
  re-testing motion against every other body.
- *chase + separation* and *wall + separation* hold TPS ≈ 60 up to 500 bots with a jitter index of
  ~0.49 / flips ~18 % at 500 — roughly **half** of variant A's shimmer at the same counts.
- *projectiles + hurtboxes* behaves like in A: the chase pile eats the projectiles, the alive count
  collapses to ~5 at 200+ bots (242 at 100), TPS stays 60.
- The cost shows up as spikes: single physics iterations reach 25–90 ms at the high counts while the
  per-window *average* stays at 5–23 ms — the frame absorbs them, but the frame time is less even
  than in variant A. Collision pairs run slightly higher than A (204 vs 136 at 500 chase bots) since
  every rigid body stays permanently active.
