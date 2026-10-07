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
}
