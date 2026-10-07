namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The collision layers of the benchmark. Enemy-enemy and player-enemy contact is a layer/mask question
/// so that both variants switch it the same way; soft separation works without any of these pairs.
/// </summary>
public static class BenchLayers
{
    public const uint Walls = 1u << 0;
    public const uint Player = 1u << 1;
    public const uint Enemies = 1u << 2;
    public const uint Hurtboxes = 1u << 3;
    public const uint Projectiles = 1u << 4;
}
