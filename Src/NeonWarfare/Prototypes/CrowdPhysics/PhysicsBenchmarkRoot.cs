using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The benchmark conductor: builds the arena, the bodies and the HUD, runs the per-tick pipeline
/// (steering, soft separation, knockback decay, velocity prescription), handles the input toggles and
/// feeds the metrics. It knows the bodies only through the IPhysicsVariant contracts, so it stays
/// byte-identical between the two prototype branches.
/// </summary>
public partial class PhysicsBenchmarkRoot : Node2D
{
    private const float ZoomStep = 1.15f;
    private const float ZoomMin = 0.15f;
    private const float ZoomMax = 3f;
    private const float WanderArrivalDistance = 40f;
    private const float OutsideMargin = 60f;

    [Logger] private ILogger _log;

    private readonly List<BotUnit> _bots = [];
    private readonly Dictionary<Node, BotUnit> _botByBodyNode = [];
    private readonly List<BenchWall> _runtimeWalls = [];
    private readonly SpatialHashGrid _grid = new(2f * BenchSpecs.BotRadius);
    private readonly List<int> _neighbours = [];
    private readonly BenchmarkMetrics _metrics = new();
    private readonly RandomNumberGenerator _rng = new() { Seed = BenchSpecs.RandomSeed };

    private IPhysicsVariant _variant;
    private IBenchPlayerBody _player;
    private Camera2D _camera;
    private BenchArena _arena;
    private Node2D _bodiesContainer;
    private Node2D _runtimeWallsContainer;
    private Node2D _projectilesContainer;
    private BenchmarkHud _hud;
    private AutoBenchmark _auto;

    private int _initialBotCount = BenchSpecs.DefaultBotCount;
    private bool _autoEnabled;
    private float _zoom = 1f;
    private float _playerFacing;
    private float _projectileSpawnRemainder;
    private int _projectilesAlive;
    private double _hudAccumulator;
    private double _logAccumulator;

    private SteeringMode _steering = SteeringMode.Chase;
    private bool _playerBlocksEnemies;
    private bool _enemiesSelfCollide;
    private bool _projectilesEnabled;
    private bool _hurtboxesEnabled;
    private int _projectileRate = BenchSpecs.DefaultProjectileRate;

    public IPhysicsVariant Variant => _variant;

    /// <summary>Called by the starter before the node enters the tree: plain parameters, no cmd args here.</summary>
    public void InitBenchArgs(int botCount, bool autoBenchmark)
    {
        _initialBotCount = botCount;
        _autoEnabled = autoBenchmark;
    }

    public override void _Ready()
    {
        Di.Process(this);

        _variant = DiscoverVariant();
        if (_variant == null)
        {
            _log.Error(
                "No IPhysicsVariant implementation found in the assembly — this branch must add its variant code");
            Callable.From(() => GetTree().Quit(1)).CallDeferred();
            return;
        }

        BuildTree();

        _metrics.WindowCompletedEvent += OnWindowCompleted;
        if (_autoEnabled)
        {
            _auto = new AutoBenchmark(this);
            _auto.Start();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_variant == null)
        {
            return;
        }

        _metrics.OnTickStart();
        ulong startUsec = Time.GetTicksUsec();
        float dt = (float)delta;

        Vector2 moveInput = Vector2.Zero;
        if (Input.IsKeyPressed(Key.W))
        {
            moveInput.Y -= 1f;
        }
        if (Input.IsKeyPressed(Key.S))
        {
            moveInput.Y += 1f;
        }
        if (Input.IsKeyPressed(Key.A))
        {
            moveInput.X -= 1f;
        }
        if (Input.IsKeyPressed(Key.D))
        {
            moveInput.X += 1f;
        }
        _player.ApplyVelocity(moveInput.LimitLength(1f) * BenchSpecs.PlayerSpeed);

        SteerBots();
        SeparateBots();

        float knockbackDecay = MathF.Exp(-MathF.Log(2f) * dt / BenchSpecs.KnockbackHalfLife);
        foreach (BotUnit unit in _bots)
        {
            unit.Knockback *= knockbackDecay;
            unit.Body.ApplyVelocity(unit.Intent + unit.Separation + unit.Knockback);

            Vector2 actualVelocity = unit.Body.LastDisplacement / dt;
            _metrics.AddBotMovement(actualVelocity, unit.PreviousActualVelocity, unit.MaxSpeed);
            unit.PreviousActualVelocity = actualVelocity;
        }

        if (_projectilesEnabled)
        {
            SpawnProjectiles(dt);
        }
        _metrics.AddProjectilesAlive(_projectilesAlive);

        _metrics.AddTickCallbackTime((Time.GetTicksUsec() - startUsec) / 1000f);
    }

    public override void _Process(double delta)
    {
        if (_variant == null)
        {
            return;
        }

        _playerFacing = (GetGlobalMousePosition() - _player.Position).Angle();
        _player.SetFacing(_playerFacing);
        _camera.Position = _player.Position;
        _camera.Zoom = new Vector2(_zoom, _zoom);

        _metrics.OnFrame();
        _auto?.OnFrame(delta);

        _hudAccumulator += delta;
        if (_hudAccumulator >= 0.5)
        {
            _hudAccumulator = 0;
            _hud.SetText(BuildHudText());
        }

        _logAccumulator += delta;
        if (_logAccumulator < 1.0)
        {
            return;
        }

        _logAccumulator = 0;
        if (_metrics.LastWindow is not { } window)
        {
            return;
        }

        _log.Information(
            "TPS {tps:F1} | physics {engineAvg:F2}/{engineP95:F2}/{engineMax:F2} ms | " +
            "callbacks {callbackAvg:F2}/{callbackMax:F2} ms | FPS {fps:F0} | jitter {jitter:F4} | " +
            "flips {flips:P1} | pairs {pairs:F0} | bots {bots} | projectiles {projectiles} | outside {outside}",
            window.Tps, window.EnginePhysicsMsAvg, window.EnginePhysicsMsP95, window.EnginePhysicsMsMax,
            window.TickCallbackMsAvg, window.TickCallbackMsMax, window.Fps, window.JitterIndex,
            window.FlipShare, window.CollisionPairsAvg, _bots.Count, _projectilesAlive, CountBotsOutsideArena());
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        // In the automatic benchmark every state change belongs to the runner; manual keys would only
        // make the two variants incomparable.
        if (_variant == null || _auto != null)
        {
            return;
        }

        if (@event is InputEventKey { Pressed: true } key)
        {
            switch (key.Keycode)
            {
                case Key.Key1:
                    SetSteering(SteeringMode.Chase);
                    break;
                case Key.Key2:
                    SetSteering(SteeringMode.Wall);
                    break;
                case Key.Key3:
                    SetSteering(SteeringMode.Wander);
                    break;
                case Key.Equal or Key.KpAdd:
                    SetBotCount(_bots.Count + BenchSpecs.BotsStep);
                    break;
                case Key.Minus or Key.KpSubtract:
                    SetBotCount(_bots.Count - BenchSpecs.BotsStep);
                    break;
                case Key.B:
                    _playerBlocksEnemies = !_playerBlocksEnemies;
                    ApplyPlayerBlocking();
                    break;
                case Key.C:
                    _enemiesSelfCollide = !_enemiesSelfCollide;
                    ApplySelfCollisions();
                    break;
                case Key.P:
                    _projectilesEnabled = !_projectilesEnabled;
                    break;
                case Key.H:
                    _hurtboxesEnabled = !_hurtboxesEnabled;
                    ApplyHurtboxes();
                    break;
                case Key.Bracketleft:
                    _projectileRate = Math.Max(
                        BenchSpecs.ProjectileRateStep, _projectileRate - BenchSpecs.ProjectileRateStep);
                    break;
                case Key.Braceright:
                    _projectileRate += BenchSpecs.ProjectileRateStep;
                    break;
                case Key.F:
                    CreateRuntimeWall();
                    break;
                case Key.G:
                    RemoveLastRuntimeWall();
                    break;
                case Key.R:
                    ResetBots();
                    break;
            }

            return;
        }

        if (@event is InputEventMouseButton { Pressed: true } mouse)
        {
            switch (mouse.ButtonIndex)
            {
                case MouseButton.Right:
                    ExplodeAtCursor();
                    break;
                case MouseButton.WheelUp:
                    SetZoom(_zoom * ZoomStep);
                    break;
                case MouseButton.WheelDown:
                    SetZoom(_zoom / ZoomStep);
                    break;
            }
        }
    }

    public void ApplyAutoSetup(SteeringMode steering, bool selfCollisions, bool projectiles, bool hurtboxes)
    {
        SetSteering(steering);
        _enemiesSelfCollide = selfCollisions;
        ApplySelfCollisions();
        _projectilesEnabled = projectiles;
        _hurtboxesEnabled = hurtboxes;
        ApplyHurtboxes();
    }

    public void SetBotCount(int count)
    {
        count = Math.Max(0, count);
        while (_bots.Count < count)
        {
            SpawnBot();
        }
        while (_bots.Count > count)
        {
            RemoveLastBot();
        }
    }

    public void ResetMetricAccumulators()
    {
        _metrics.ResetAccumulators();
    }

    public void FinishBenchmark(string csvPath)
    {
        _log.Information("Benchmark CSV written to {csvPath}", csvPath);
        Callable.From(() => GetTree().Quit()).CallDeferred();
    }

    public void NotifyProjectileHitBot(IBenchBotBody bot, Vector2 direction)
    {
        if (_botByBodyNode.TryGetValue(bot.Node, out BotUnit unit))
        {
            unit.Knockback = (unit.Knockback + direction * BenchSpecs.ProjectileKnockback)
                .LimitLength(BenchSpecs.ExplosionKnockback);
        }
    }

    public void NotifyProjectileDied(BenchProjectile projectile)
    {
        _projectilesAlive--;
    }

    private static IPhysicsVariant DiscoverVariant()
    {
        List<Type> matches = typeof(PhysicsBenchmarkRoot).Assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && typeof(IPhysicsVariant).IsAssignableFrom(type))
            .ToList();

        return matches.Count == 1 ? (IPhysicsVariant)Activator.CreateInstance(matches[0]) : null;
    }

    private void BuildTree()
    {
        _camera = new Camera2D();
        AddChild(_camera);
        _camera.MakeCurrent();

        _arena = new BenchArena();
        AddChild(_arena);

        _bodiesContainer = new Node2D();
        AddChild(_bodiesContainer);
        _runtimeWallsContainer = new Node2D();
        AddChild(_runtimeWallsContainer);
        _projectilesContainer = new Node2D();
        AddChild(_projectilesContainer);

        _hud = new BenchmarkHud();
        AddChild(_hud);

        _player = _variant.CreatePlayer();
        ApplyPlayerBlocking();
        _bodiesContainer.AddChild(_player.Node);
        _player.PlaceAt(Vector2.Zero);

        for (int i = 0; i < _initialBotCount; i++)
        {
            SpawnBot();
        }
    }

    private void OnWindowCompleted(MetricsWindow window)
    {
        _auto?.OnWindow(window);
    }

    private void SteerBots()
    {
        Vector2 playerPosition = _player.Position;
        foreach (BotUnit unit in _bots)
        {
            Vector2 target = _steering switch
            {
                SteeringMode.Chase => playerPosition,
                SteeringMode.Wall => BenchArena.WallCoverPoint,
                _ => unit.WanderTarget,
            };

            Vector2 offset = target - unit.Body.Position;
            if (_steering == SteeringMode.Wander && offset.Length() < WanderArrivalDistance)
            {
                unit.WanderTarget = RandomArenaPoint();
                offset = unit.WanderTarget - unit.Body.Position;
            }

            unit.Intent = offset.LengthSquared() > 1f
                ? offset.Normalized() * unit.MaxSpeed
                : Vector2.Zero;
        }
    }

    private void SeparateBots()
    {
        float range = 2f * BenchSpecs.BotRadius;
        _grid.Rebuild(_bots);

        for (int i = 0; i < _bots.Count; i++)
        {
            BotUnit unit = _bots[i];
            _grid.CollectNeighbours(unit.Body.Position, range, _neighbours);

            Vector2 push = Vector2.Zero;
            foreach (int neighbourIndex in _neighbours)
            {
                if (neighbourIndex == i)
                {
                    continue;
                }

                Vector2 offset = unit.Body.Position - _bots[neighbourIndex].Body.Position;
                float distance = offset.Length();
                if (distance < 0.01f)
                {
                    // Perfectly stacked bots have no direction to separate along; nudge them randomly
                    // so a pile does not fuse into one point forever.
                    push += RandomUnitVector() * BenchSpecs.SeparationStrength * 0.5f;
                    continue;
                }

                float overlap = range - distance;
                if (overlap > 0f)
                {
                    push += offset / distance
                            * (BenchSpecs.SeparationStrength * overlap / range);
                }
            }

            unit.Separation = push.LimitLength(BenchSpecs.SeparationClamp);
        }
    }

    private void SpawnProjectiles(float delta)
    {
        _projectileSpawnRemainder += _projectileRate * delta;
        while (_projectileSpawnRemainder >= 1f)
        {
            _projectileSpawnRemainder -= 1f;
            SpawnProjectile();
        }
    }

    private void SpawnProjectile()
    {
        float angle = _playerFacing
                      + MathF.PI * _rng.RandfRange(
                          -BenchSpecs.ProjectileSpreadDegrees, BenchSpecs.ProjectileSpreadDegrees) / 180f;
        Vector2 direction = Vector2.FromAngle(angle);

        var projectile = new BenchProjectile(this, direction * BenchSpecs.ProjectileSpeed)
        {
            Position = _player.Position
                       + direction * (BenchSpecs.PlayerRadius + BenchSpecs.ProjectileRadius + 2f),
            CollisionMask = BenchLayers.Walls
                            | (_hurtboxesEnabled ? BenchLayers.Hurtboxes : BenchLayers.Enemies),
        };
        _projectilesContainer.AddChild(projectile);
        _projectilesAlive++;
    }

    private void SpawnBot()
    {
        IBenchBotBody body = _variant.CreateBot();
        body.SetCollidesWithEnemies(_enemiesSelfCollide);
        body.SetBlocksPlayer(_playerBlocksEnemies);
        body.SetHurtboxEnabled(_hurtboxesEnabled);
        _bodiesContainer.AddChild(body.Node);

        BotUnit unit = new()
        {
            Body = body,
            MaxSpeed = _rng.RandfRange(BenchSpecs.BotSpeedMin, BenchSpecs.BotSpeedMax),
            WanderTarget = RandomArenaPoint(),
        };
        body.PlaceAt(RandomFreePoint());
        _bots.Add(unit);
        _botByBodyNode[body.Node] = unit;
    }

    private void RemoveLastBot()
    {
        BotUnit unit = _bots[^1];
        _bots.RemoveAt(_bots.Count - 1);
        _botByBodyNode.Remove(unit.Body.Node);
        unit.Body.Node.QueueFree();
    }

    private void SetSteering(SteeringMode steering)
    {
        _steering = steering;
        foreach (BotUnit unit in _bots)
        {
            unit.WanderTarget = RandomArenaPoint();
        }
    }

    private void ApplyPlayerBlocking()
    {
        _player.SetBlocksEnemies(_playerBlocksEnemies);
        foreach (BotUnit unit in _bots)
        {
            unit.Body.SetBlocksPlayer(_playerBlocksEnemies);
        }
    }

    private void ApplySelfCollisions()
    {
        foreach (BotUnit unit in _bots)
        {
            unit.Body.SetCollidesWithEnemies(_enemiesSelfCollide);
        }
    }

    private void ApplyHurtboxes()
    {
        foreach (BotUnit unit in _bots)
        {
            unit.Body.SetHurtboxEnabled(_hurtboxesEnabled);
        }
    }

    private void CreateRuntimeWall()
    {
        BenchWall wall = new()
        {
            Position = GetGlobalMousePosition(),
            Rotation = _playerFacing,
        };
        _runtimeWallsContainer.AddChild(wall);
        wall.ResetPhysicsInterpolation();
        _runtimeWalls.Add(wall);
    }

    private void RemoveLastRuntimeWall()
    {
        if (_runtimeWalls.Count == 0)
        {
            return;
        }

        BenchWall wall = _runtimeWalls[^1];
        _runtimeWalls.RemoveAt(_runtimeWalls.Count - 1);
        wall.QueueFree();
    }

    private void ResetBots()
    {
        foreach (BotUnit unit in _bots)
        {
            unit.Body.PlaceAt(RandomFreePoint());
            unit.Knockback = Vector2.Zero;
            unit.PreviousActualVelocity = Vector2.Zero;
            unit.WanderTarget = RandomArenaPoint();
        }
    }

    private void ExplodeAtCursor()
    {
        Vector2 center = GetGlobalMousePosition();
        foreach (BotUnit unit in _bots)
        {
            Vector2 offset = unit.Body.Position - center;
            float distance = offset.Length();
            if (distance > BenchSpecs.ExplosionRadius)
            {
                continue;
            }

            Vector2 direction = distance > 0.5f ? offset / distance : RandomUnitVector();
            float magnitude = BenchSpecs.ExplosionKnockback
                              * (1f - distance / BenchSpecs.ExplosionRadius);
            unit.Knockback = (unit.Knockback + direction * magnitude)
                .LimitLength(BenchSpecs.ExplosionKnockback);
        }
    }

    private void SetZoom(float zoom)
    {
        _zoom = Math.Clamp(zoom, ZoomMin, ZoomMax);
    }

    private Vector2 RandomArenaPoint() => new(
        _rng.RandfRange(-BenchSpecs.ArenaHalfExtents.X + 60f, BenchSpecs.ArenaHalfExtents.X - 60f),
        _rng.RandfRange(-BenchSpecs.ArenaHalfExtents.Y + 60f, BenchSpecs.ArenaHalfExtents.Y - 60f));

    private Vector2 RandomFreePoint()
    {
        Vector2 point = RandomArenaPoint();
        for (int attempt = 0; attempt < 100 && !_arena.IsPointFree(point, BenchSpecs.BotRadius); attempt++)
        {
            point = RandomArenaPoint();
        }
        return point;
    }

    private Vector2 RandomUnitVector()
    {
        float angle = _rng.RandfRange(0f, MathF.Tau);
        return Vector2.FromAngle(angle);
    }

    private int CountBotsOutsideArena()
    {
        int outside = 0;
        foreach (BotUnit unit in _bots)
        {
            Vector2 position = unit.Body.Position;
            if (MathF.Abs(position.X) > BenchSpecs.ArenaHalfExtents.X + OutsideMargin
                || MathF.Abs(position.Y) > BenchSpecs.ArenaHalfExtents.Y + OutsideMargin)
            {
                outside++;
            }
        }
        return outside;
    }

    private string BuildHudText()
    {
        string metrics = _metrics.LastWindow is not { } window
            ? "warming up..."
            : $"FPS {window.Fps:F0} | TPS {window.Tps:F1} | cap {window.FramesAtCap}/{Engine.MaxPhysicsStepsPerFrame}\n"
              + $"physics {window.EnginePhysicsMsAvg:F2}/{window.EnginePhysicsMsP95:F2}"
              + $"/{window.EnginePhysicsMsMax:F2} ms"
              + $" | callbacks {window.TickCallbackMsAvg:F2}/{window.TickCallbackMsP95:F2}/"
              + $"{window.TickCallbackMsMax:F2} ms\n"
              + $"objects {window.ActiveObjectsAvg:F0} | pairs {window.CollisionPairsAvg:F0}"
              + $" | islands {window.IslandsAvg:F0}\n"
              + $"jitter {window.JitterIndex:F4} | flips {window.FlipShare:P1} | outside {CountBotsOutsideArena()}";

        return $"{_variant.Title} | bots {_bots.Count} | projectiles {_projectilesAlive}"
               + $" | walls {_runtimeWalls.Count}\n"
               + $"steer {_steering} | B {_playerBlocksEnemies} | C {_enemiesSelfCollide}"
               + $" | P {_projectilesEnabled} ({_projectileRate}/s) | H {_hurtboxesEnabled} | auto {_autoEnabled}\n"
               + metrics;
    }
}
