using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>A body the harness can steer and reposition, whatever physics type the variant chose.</summary>
public interface IBenchBody
{
    /// <summary>The body node, for adding it to the tree and for looking it up from physics callbacks.</summary>
    Node Node { get; }

    Vector2 Position { get; }

    /// <summary>
    /// A hard position write (respawn). Physics interpolation stays on, so the body must snap its visuals
    /// itself with ResetPhysicsInterpolation().
    /// </summary>
    void PlaceAt(Vector2 globalPosition);
}
