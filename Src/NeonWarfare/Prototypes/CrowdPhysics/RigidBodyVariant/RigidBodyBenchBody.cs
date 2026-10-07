using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics.RigidBodyVariant;

/// <summary>
/// The body both variant-B characters are built on. Every tick the harness prescribes a velocity and
/// _IntegrateForces writes it straight into the solver state — arcade control without forces — but the
/// solver's collision response of the previous step is measured (post-solve velocity minus what was
/// prescribed then), clamped and added back on top, decaying per tick. That keeps rams pushing and
/// bounce alive instead of discarding the solver's work every tick, which is what broke rams and
/// caused tunnelling in the old PhysicsCalculator.
/// </summary>
public abstract partial class RigidBodyBenchBody : RigidBody2D
{
    /// <summary>The retained solver response is clamped, so a wall hit cannot accumulate into a launch.</summary>
    private const float MaxSolverResponse = 400f;

    /// <summary>Half of the measured response survives one tick: a bump fades instead of ringing.</summary>
    private const float SolverResponseRetention = 0.5f;

    /// <summary>
    /// Continuous collision detection for knockback speeds. Off: 900 px/s is 15 px per tick against
    /// 30-40 px walls, and the automatic runs never showed a bot outside the arena; flip this on
    /// (CastShape) if a faster knockback ever tunnels.
    /// </summary>
    private const bool UseContinuousCollision = false;

    private Vector2 _desiredVelocity;
    private Vector2 _lastDesiredVelocity;
    private Vector2 _lastIntegrateOrigin;
    private Vector2? _teleportTo;

    protected RigidBodyBenchBody(float radius, uint layer)
    {
        CollisionLayer = layer;
        CollisionMask = BenchLayers.Walls;

        // Top-down arcade movement: no gravity, no rotation from contacts, no air drag, and never
        // sleep — a sleeping body would ignore the separation pushes.
        GravityScale = 0f;
        LockRotation = true;
        LinearDamp = 0f;
        CanSleep = false;
        if (UseContinuousCollision)
        {
            ContinuousCd = CcdMode.CastShape;
        }

        AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = radius },
        });
    }

    public Node Node => this;

    public Vector2 Position => GlobalPosition;

    public Vector2 LastDisplacement { get; private set; }

    public void ApplyVelocity(Vector2 velocity)
    {
        _desiredVelocity = velocity;
    }

    public void PlaceAt(Vector2 globalPosition)
    {
        _teleportTo = globalPosition;
    }

    public override void _Ready()
    {
        _lastIntegrateOrigin = GlobalPosition;
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        if (_teleportTo is Vector2 destination)
        {
            state.Transform = new Transform2D(0f, destination);
            state.LinearVelocity = Vector2.Zero;
            _lastIntegrateOrigin = destination;
            _lastDesiredVelocity = Vector2.Zero;
            LastDisplacement = Vector2.Zero;
            _teleportTo = null;
            ResetPhysicsInterpolation();
            return;
        }

        // The origin the previous integrate saw is the pre-solve position of the finished step, so the
        // difference is the body's real motion including the solver's position correction.
        LastDisplacement = state.Transform.Origin - _lastIntegrateOrigin;
        _lastIntegrateOrigin = state.Transform.Origin;

        Vector2 response = (state.LinearVelocity - _lastDesiredVelocity)
            .LimitLength(MaxSolverResponse) * SolverResponseRetention;
        state.LinearVelocity = _desiredVelocity + response;
        _lastDesiredVelocity = _desiredVelocity;
    }
}
