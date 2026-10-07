using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Harness-side state of one bot: its speed, the velocity terms the harness computes each tick (intent,
/// separation, knockback), the wander target, and the previous actual velocity for the jitter metric.
/// The physics body itself lives in the variant.
/// </summary>
public sealed class BotUnit
{
    public required IBenchBotBody Body { get; init; }
    public required float MaxSpeed { get; init; }

    public Vector2 Intent { get; set; }
    public Vector2 Separation { get; set; }

    /// <summary>Scripted knockback velocity; decays exponentially and is added to the intent velocity.</summary>
    public Vector2 Knockback { get; set; }

    public Vector2 WanderTarget { get; set; }
    public Vector2 PreviousActualVelocity { get; set; }
}
