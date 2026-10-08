using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// A projectile: an Area2D moved manually every tick, never a physics body. Its cost to the physics
/// server is the point of the measurement, so it keeps monitoring on and detects walls and bots through
/// the normal broadphase. The collision mask is owned by the harness (it changes with the hurtbox
/// toggle), the layer/mask values are set by the root right after construction.
/// </summary>
public partial class BenchProjectile : Area2D
{
    private readonly Vector2 _velocity;
    private readonly PhysicsBenchmarkRoot _root;
    private float _age;
    private bool _dead;

    public BenchProjectile(PhysicsBenchmarkRoot root, Vector2 velocity)
    {
        _root = root;
        _velocity = velocity;
    }

    public Vector2 Velocity => _velocity;

    /// <summary>Killed by the harness on a configuration reset, not by a hit.</summary>
    public void Destroy()
    {
        Die();
    }

    public override void _Ready()
    {
        CollisionLayer = BenchLayers.Projectiles;
        Monitoring = true;
        // Projectiles do not need to be seen by anything; keeping Monitorable off halves the broadphase work.
        Monitorable = false;

        AddChild(new CollisionShape2D
        {
            Shape = new CircleShape2D { Radius = BenchSpecs.ProjectileRadius },
        });

        AreaEntered += OnAreaEntered;
        BodyEntered += OnBodyEntered;
        QueueRedraw();
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += _velocity * (float)delta;
        _age += (float)delta;
        if (_age >= BenchSpecs.ProjectileLifetime)
        {
            Die();
        }
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, BenchSpecs.ProjectileRadius, new Color(1.0f, 0.6f, 0.2f));
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area.GetParent() is IBenchBotBody bot)
        {
            _root.NotifyProjectileHitBot(bot, _velocity.Normalized());
        }
        Die();
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is IBenchBotBody bot)
        {
            _root.NotifyProjectileHitBot(bot, _velocity.Normalized());
        }
        Die();
    }

    private void Die()
    {
        if (_dead)
        {
            return;
        }
        _dead = true;
        _root.NotifyProjectileDied(this);
        QueueFree();
    }
}
