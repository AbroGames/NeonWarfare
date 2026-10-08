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
"$GODOT_EXE" --path "./" --headless --physics-benchmark --bench-auto    # the legacy suite, one CSV
"$GODOT_EXE" --path "./" --headless --fixed-fps 60 --physics-benchmark --bench-suite core \
    --bench-rep 1 --bench-throughput --bench-out /tmp/bench    # one suite in throughput mode
```

`--bench-count <n>` sets the initial bot count (default 100). A suite run walks all of its
configurations and bot counts and quits after writing the CSV; the log prints the globalized path in
the `Benchmark CSV written to ...` line. All flags are documented in `Docs/Cli-args.md`.

## run-suites.sh

The whole comparison is one command from the repository root (with `GODOT_EXE` set):

```bash
./Src/NeonWarfare/Prototypes/CrowdPhysics/run-suites.sh
```

The script runs `sanity` first (a throughput-mode check, see below) and stops if it fails, then runs
`core`, `blocking`, `load`, `load-rate` and `events` in throughput mode with repetitions 1..3, then
`realtime` in realtime mode, then `events` with `--bench-ccd` for variant B only. The two variants
always run back to back of the same suite, so thermal drift hits both equally. Every variant runs
from a detached worktree (built and imported headless), logs land in `logs/`, the CSVs and an
`env.txt` with the machine facts in the output directory; the script prints one `OK <csv>` or
`FAIL <log>` line per run and never stops for a failed run (only for a failed sanity gate). Partial
reruns: `SUITES="core events"`, `REPS=1`, `VARIANTS="characterbody"`. The default full run is
roughly 1–1.5 h (assumption — the real number is machine-specific; take it from the first run).

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

In automatic mode all manual input is ignored; the player, the facing and the scripted events are
driven by the configuration.

## Suites

`sep` = soft separation only; `phys` = `C` on (physical enemy–enemy collisions, separation stays on);
`block` = `B` on. Counts `R50` = 50 → 500 step 50, `R100` = 100 → 500 step 100. Facing is
`TowardCrowd` unless noted.

| Suite | Configurations | Counts |
|---|---|---|
| `sanity` | `wander-sep` — the throughput-mode gate, nothing else | 0, 50 |
| `legacy` | the original four configurations (`chase+separation`, `wall+separation`, `chase+self-collisions`, `chase+projectiles+hurtboxes`) | R50 |
| `core` | `chase-static-sep`, `chase-static-phys`, `chase-kite-sep`, `chase-kite-phys`, `wall-sep`, `wall-phys`, `wander-sep` | R50, then 600, 800, 1000 |
| `blocking` | `chase-kite-sep-block`, `chase-kite-phys-block`, `chase-static-sep-block`, `chase-static-phys-block` | R100 |
| `load` | `wander-sep-hurt`, `wander-sep-proj` (300/s, Spin), `wander-sep-proj-hurt` (Spin), `chase-kite-sep-proj-hurt`, `chase-kite-phys-proj-hurt` | R50 |
| `load-rate` | `wander-sep-hurt-p300`, `-p600`, `-p900`, `-p1200` (projectiles per second, Spin) | 300 |
| `events` | `chase-kite-sep-boom`, `chase-kite-phys-boom` (explosions at the player), `wall-sep-boom`, `wall-phys-boom` (explosions into the wall — the tunnelling test), `chase-static-sep-walldrop`, `chase-static-phys-walldrop` | R100 |
| `realtime` | `chase-kite-sep`, `chase-kite-phys`, `chase-kite-sep-proj-hurt`, `chase-kite-phys-proj-hurt` | 300, 500 |

The scripted behaviour of the auto mode:

- **Kite** — the player walks a closed ellipse (centre `(0, 300)`, radii `600 × 450`) around the
  central box. A carrot point advances along the ellipse at 300 px/s only while the player is within
  80 px of it, so a blocked player is not dragged through the crowd and `player_speed_ratio` says how
  much of the commanded speed actually came through.
- **Facing** — `TowardCrowd` faces the bot centroid; `Spin` rotates at 90°/s (sprays projectiles over
  a spread-out wander crowd).
- **Explosions** — `AtPlayer`: every 1.5 s at the player position, same radius/strength as RMB.
  `IntoWall` (for `wall` steering): bots pressing the south face of the long wall (`y ∈ [-480, -230]`,
  `x ∈ [-1600, 600]`) are blown north into the wall from a point 150 px south of their centroid, at
  up to 900 px/s — the tunnelling test.
- **Wall drop** — every 2 s a runtime wall (300 × 30) is created at the centroid of the bots within
  300 px of the player (rotation alternating 0°/90°) and removed after 1 s: the "wall created on top
  of bots" test. The `stuck_max` column reports bots inside a wall that has existed ≥ 0.5 s (younger
  walls are excluded: being depenetrated out of a fresh wall is transient and expected).

Every configuration starts from a clean arena: bots respawned, the player back at the start, runtime
walls and projectiles gone, knockback zeroed.

## Metric windows and the CSV

A metric window is one wall-clock second in realtime mode, or 60 physics ticks (1 s of game time) in
throughput mode. A step = 2 s settle + 5 windows, i.e. the same 420 ticks of game time in both modes.
One CSV row per step; rows are appended and flushed immediately, so a killed run keeps everything
measured so far. The columns, in file order:

| Column | Definition |
|---|---|
| `variant`, `suite`, `config`, `rep`, `mode`, `mode_ok`, `ccd`, `seed` | Run identification: `mode` = throughput/realtime, `mode_ok` = the throughput sanity flag (always 1 in realtime), `seed` = `RandomSeed + rep` |
| `bots`, `projectile_rate`, `projectiles_alive` | Counts: configured rate (0 = off) and the average alive |
| `tps` | Physics ticks per wall-clock second, averaged over the step's windows |
| `tick_full_ms` | Throughput only: window wall time / ticks — the full cost of one physics tick (callbacks + physics server step + near-zero headless render), identical in meaning for both variants. Empty in realtime |
| `frame_ms_p50`, `frame_ms_p99`, `frame_ms_max` | Wall time between consecutive `_Process` calls: per-step percentiles over all frame samples of the step. In throughput mode this is the per-tick spike distribution |
| `engine_max_ms` | The max of `Performance.Monitor.TimePhysicsProcess` over the step. The monitor holds the *running max* physics-iteration time of each wall-clock second (server step included, see the variant section), so this is a spike metric |
| `callbacks_ms_avg`, `callbacks_ms_p95`, `callbacks_ms_max` | The harness's own wall time around its per-tick work. Not comparable between variants by itself: in A it contains all of `MoveAndSlide`, in B the solver runs in the server step outside it |
| `fps`, `frames_at_cap` | Render rate (meaningless in throughput mode) and frames that hit the physics catch-up cap |
| `jitter_index`, `flip_share` | As before: mean `|v(t) − v(t−1)| / maxSpeed` over bots (`v` = actual displacement per tick / step), and the share of bots whose movement direction flipped by more than 120° between ticks |
| `overlap_mean_px`, `overlap_max_px` | Bot–bot penetration `2·BotRadius − distance` over pairs closer than that: the mean is over overlapping pairs per tick, averaged over the ticks that had any; the max is the worst pair over the step |
| `player_overlap_max_px` | Worst penetration of a bot into the player over the step |
| `player_speed_ratio` | Kite only: mean actual player speed / commanded speed over ticks with a nonzero command; empty for `Static` |
| `stuck_max`, `outside_max` | Worst sampled count (every 10 ticks) of bots inside static/runtime (≥ 0.5 s) wall geometry, and outside the arena + margin |
| `max_bot_speed`, `flung` | Max actual bot speed (px/s) over the step and the number of bot-ticks above 1600 px/s (the legit max is 250 move + 250 separation + 900 knockback, + 200 of B's retained response) |
| `collision_pairs`, `active_objects`, `islands` | The `Performance` physics monitor averages |
| `gc_gen0`, `alloc_kb_per_tick` | Gen-0 collections and managed bytes (main thread) over the measurement phase, per tick |
| `early_stop` | 0 = complete step; 1 = this step passed the early-stop threshold, the rest of the configuration's counts were skipped; 2 = the watchdog ended the step |

## Throughput mode

The fair cost metric. With Godot's `--fixed-fps 60` every frame advances exactly 1/60 s of game time
and runs one physics tick, without real-time sync, as fast as the CPU allows — the wall time per
frame is then the full cost of a tick, the same quantity for A (whose movement happens inside the
tick callback) and B (whose solver runs in the physics server step). `tick_full_ms` is that number.

- At startup the benchmark sets `OS.LowProcessorUsageModeSleepUsec = 0`: when the display server
  cannot draw (headless), each frame is padded to the low-processor sleep (~6.9 ms), which would cap
  the loop at ~145 frames/s.
- The first completed window must reach 70 TPS; if not, the run was throttled (typically
  `--fixed-fps 60` missing) and every row gets `mode_ok = 0`. The `sanity` suite exists to check
  exactly this before the real runs.

Two assumptions could not be verified without running Godot 4.7 and are covered by the sanity suite:
that `--fixed-fps 60` yields exactly one physics tick per frame in this Godot version, and that the
sleep override actually removes the headless frame cap.

## Early stop and watchdog

After recording a step: if `tick_full_ms > 50` (throughput) or `tps < 30` (realtime), the remaining
bot counts of that configuration are skipped and the row is marked `early_stop = 1` — a step that
slow cannot get faster by adding bots. A step that takes more than 90 s of wall time records what it
has (`early_stop = 2`, or no row if not even one window completed) and its configuration is skipped.

## Harness layout

| File | What it is |
|---|---|
| `PhysicsBenchmarkRoot.cs` | The conductor: builds the tree, runs the per-tick pipeline, input, scripted auto behaviour, metrics |
| `IPhysicsVariant.cs` + `IBenchBody.cs` / `IBenchPlayerBody.cs` / `IBenchBotBody.cs` | The contracts a variant implements; the root discovers the single implementation in the assembly |
| `BenchConfig.cs` / `BenchSuites.cs` | The configuration model and the named suites |
| `BenchRunOptions.cs` | The run-wide options mapped from the command line |
| `AutoBenchmark.cs` | The suite walk, early stop, watchdog, and the CSV (append + flush per row) |
| `BenchmarkMetrics.cs` / `MetricsWindow.cs` | The metric windows (one second / 60 ticks) |
| `BotUnit.cs` | Harness-side per-bot state (intent, separation, knockback, wander target, previous velocity) |
| `BenchArena.cs` / `BenchWall.cs` | Static geometry with spawn-freeness checks; runtime walls |
| `BenchProjectile.cs` | Manually-moved `Area2D` projectiles |
| `SpatialHashGrid.cs` | The uniform grid behind the O(N) soft separation |
| `BenchmarkHud.cs` | The overlay |
| `BenchSpecs.cs` / `BenchLayers.cs` | Every tunable number and collision layer in one place |
| `run-suites.sh` | The full two-variant comparison in one command |

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
- **CCD is a runtime toggle (`--bench-ccd`), off by default.** Knockback peaks at 900 px/s = 15 px
  per tick against 30–40 px walls, and no bot ever left the arena in the earlier automatic runs
  (`outside 0` everywhere), so the default measures the cheaper configuration. With the flag the
  bodies switch to `CcdMode.CastShape`; `run-suites.sh` reruns the `events` suite with it (variant B
  only — for A the flag is a no-op) to put a number on what the sweep costs.
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
