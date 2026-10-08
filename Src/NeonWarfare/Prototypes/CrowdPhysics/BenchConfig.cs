using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

public enum PlayerMotionMode
{
    /// <summary>The player stays at its start point (the static pile of chase mode).</summary>
    Static,

    /// <summary>The player walks a closed ellipse around the central box, dragging the chasing crowd along.</summary>
    Kite,
}

public enum FacingMode
{
    /// <summary>Toward the centroid of all bots.</summary>
    TowardCrowd,

    /// <summary>Rotates at 90 deg/s, spraying projectiles over a spread-out crowd.</summary>
    Spin,
}

public enum ExplosionMode
{
    None,

    /// <summary>Every ExplosionPeriod at the player position: the hugging crowd is blown off.</summary>
    AtPlayer,

    /// <summary>Every ExplosionPeriod south of the long wall, throwing its crowd north into the wall —
    /// the tunnelling test.</summary>
    IntoWall,
}

/// <summary>
/// Everything that distinguishes one benchmark configuration from another. Applied wholesale at the
/// start of the configuration (not between bot-count steps), so every configuration starts from the
/// same clean arena instead of the previous one's pile.
/// </summary>
public sealed record BenchConfig(
    string Name,
    SteeringMode Steering,
    PlayerMotionMode PlayerMotion,
    bool SelfCollisions,
    bool PlayerBlocks,
    bool Hurtboxes,
    int ProjectileRate,
    FacingMode Facing,
    ExplosionMode Explosions,
    double WallDropPeriod,
    double WallDropLifetime,
    int[] Counts);
