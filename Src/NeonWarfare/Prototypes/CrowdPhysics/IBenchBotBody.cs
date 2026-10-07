using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>A bot body of a variant. The harness prescribes a velocity per tick; the body realizes it
/// with its own physics type and reports what actually moved, which is what the jitter metrics read.</summary>
public interface IBenchBotBody : IBenchBody
{
    /// <summary>Actual displacement of the last movement, collision response included.</summary>
    Vector2 LastDisplacement { get; }

    /// <summary>Sets this tick's velocity (`intent + separation + knockback`) and performs the movement.</summary>
    void ApplyVelocity(Vector2 velocity);

    /// <summary>The `C` toggle, bot side: physical enemy-enemy collisions (the baseline-jitter mode).</summary>
    void SetCollidesWithEnemies(bool enabled);

    /// <summary>The `B` toggle, bot side: whether this bot collides with the player.</summary>
    void SetBlocksPlayer(bool enabled);

    /// <summary>The `H` toggle: the extra Area2D hurtbox child, as the real character will have.</summary>
    void SetHurtboxEnabled(bool enabled);
}
