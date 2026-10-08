using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Every tunable of the benchmark in one place. Both variants must read them from here, so a changed
/// number can never make the comparison unfair.
/// </summary>
public static class BenchSpecs
{
    /// <summary>One seed for both variants: the same spawn points and the same wander targets.</summary>
    public const long RandomSeed = 20261007L;

    public const float PlayerRadius = 16f;
    public const float PlayerSpeed = 300f;

    public const float BotRadius = 14f;
    public const float BotSpeedMin = 150f;
    public const float BotSpeedMax = 250f;

    public static readonly Vector2 ArenaHalfExtents = new(2000f, 1500f);
    public const float WallThickness = 40f;

    public const float ExplosionRadius = 250f;
    public const float ExplosionKnockback = 900f;
    public const float KnockbackHalfLife = 0.15f;
    public const float ProjectileKnockback = 150f;

    public const float SeparationStrength = 400f;
    public const float SeparationClamp = 250f;

    public const float ProjectileSpeed = 600f;
    public const float ProjectileRadius = 3f;
    public const float ProjectileLifetime = 2f;
    public const float ProjectileSpreadDegrees = 12f;
    public const int DefaultProjectileRate = 300;
    public const int ProjectileRateStep = 50;

    public const int DefaultBotCount = 100;
    public const int BotsStep = 50;

    // --- Scripted events (auto mode) ---

    public const float ExplosionPeriod = 1.5f;
    public const float WallDropPeriod = 2f;
    public const float WallDropLifetime = 1f;

    /// <summary>The wall drop is placed at the centroid of the bots within this radius of the player.</summary>
    public const float WallDropCrowdRadius = 300f;

    /// <summary>
    /// The band south of the long wall (the wall itself is `y` in [-520, -480], `x` in [-1600, 600] —
    /// see BenchArena) whose bots count as "pressing the wall" for the IntoWall explosion.
    /// </summary>
    public static readonly Vector2 IntoWallBandMin = new(-1600f, -480f);
    public static readonly Vector2 IntoWallBandMax = new(600f, -230f);

    /// <summary>IntoWall explodes only when at least this many bots press the wall.</summary>
    public const int IntoWallMinBots = 5;

    /// <summary>The IntoWall blast goes this far south of the pressing crowd's centroid, so the bots
    /// north of the blast are thrown north into the wall.</summary>
    public const float IntoWallBlastOffset = 150f;

    // --- Kite player motion: a closed ellipse around the central box ---

    public static readonly Vector2 KiteCentre = new(0f, 300f);
    public static readonly Vector2 KiteRadii = new(600f, 450f);

    /// <summary>The carrot advances along the ellipse only while the player is this close to it.</summary>
    public const float KiteCarrotDistance = 80f;

    // --- Metric thresholds and sampling ---

    /// <summary>
    /// Actual speeds above this are flagged as "flung": the legit maximum is 250 move + 250 separation
    /// + 900 knockback, plus up to 200 of variant B's retained solver response.
    /// </summary>
    public const float FlungSpeedThreshold = 1600f;

    /// <summary>A runtime wall younger than this does not count for the stuck metric: bots inside a
    /// just-dropped wall are being depenetrated, which is transient and expected.</summary>
    public const float StuckWallMinAge = 0.5f;

    public const int StuckSampleIntervalTicks = 10;

    // --- Automatic benchmark ---

    /// <summary>In throughput mode a metric window is this many physics ticks (1 s of game time at 60 TPS).</summary>
    public const int ThroughputWindowTicks = 60;

    /// <summary>The throughput-mode sanity check: a slower first window means --fixed-fps 60 was not passed.</summary>
    public const float ThroughputMinTps = 70f;

    /// <summary>Early stop: a step this expensive ends its configuration (throughput mode).</summary>
    public const float EarlyStopTickMs = 50f;

    /// <summary>Early stop: a step this slow ends its configuration (realtime mode).</summary>
    public const float EarlyStopRealtimeTps = 30f;

    /// <summary>A step (settle + measurement) may not take more wall time than this.</summary>
    public const float StepWatchdogSeconds = 90f;
}
