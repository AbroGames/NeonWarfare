namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// Variant B: the player and the bots are RigidBody2D. This is the only IPhysicsVariant implementation
/// on this branch; the harness discovers it by type.
/// </summary>
public sealed class RigidBodyVariant : IPhysicsVariant
{
    public string Title => "B - RigidBody2D";

    public string FileStamp => "rigidbody";

    public IBenchPlayerBody CreatePlayer() => new RigidBodyBenchPlayer();

    public IBenchBotBody CreateBot() => new RigidBodyBenchBot();
}
