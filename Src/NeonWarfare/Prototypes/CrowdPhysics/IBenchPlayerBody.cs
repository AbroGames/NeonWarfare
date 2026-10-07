using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>The player body of a variant: moved by direct velocity, blocking is a mask question.</summary>
public interface IBenchPlayerBody : IBenchBody
{
    void ApplyVelocity(Vector2 velocity);

    /// <summary>Rotation is visual only; the body itself is never spun up by the physics.</summary>
    void SetFacing(float angleRadians);

    /// <summary>The `B` toggle, player side: whether enemies collide with this body.</summary>
    void SetBlocksEnemies(bool enabled);
}
