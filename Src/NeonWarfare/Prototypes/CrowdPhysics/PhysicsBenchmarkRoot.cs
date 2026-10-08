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
    private readonly List<RuntimeWall> _runtimeWalls = [];
    private readonly SpatialHashGrid _grid = new(2f * BenchSpecs.BotRadius);
    private readonly List<int> _neighbours = [];
    private BenchmarkMetrics _metrics = new(false);
    private readonly RandomNumberGenerator _rng = new();

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
    private BenchRunOptions _options = new(null, null, 1, false, false);
    private float _zoom = 1f;
    private float _playerFacing;
    private float _projectileSpawnRemainder;
    private int _projectilesAlive;
    private double _hudAccumulator;
    private double _logAccumulator;
    private int _sampleTickCounter;

    private SteeringMode _steering = SteeringMode.Chase;
    private bool _playerBlocksEnemies;
    private bool _enemiesSelfCollide;
    private bool _projectilesEnabled;
    private bool _hurtboxesEnabled;
    private int _projectileRate = BenchSpecs.DefaultProjectileRate;

    // The scripted auto-mode behaviour, all of it driven by the current BenchConfig; in manual mode the
    // defaults never fire (no explosions, no wall drops, the player from the keyboard, facing from the mouse).
    private PlayerMotionMode _playerMotion = PlayerMotionMode.Static;
    private FacingMode _facingMode = FacingMode.TowardCrowd;
    private ExplosionMode _explosions = ExplosionMode.None;
    private double _explosionTimer;
    private double _wallDropPeriod;
    private double _wallDropTimer;
    private bool _wallDropHorizontal;
    private float _kiteAngle;
    private Vector2 _playerPreviousPosition = Vector2.Zero;

    public IPhysicsVariant Variant => _variant;

    /// <summary>The auto benchmark is a plain class and reports through the root's logger.</summary>
    internal ILogger Log => _log;

    /// <summary>Called by the starter before the node enters the tree: plain parameters, no cmd args here.</summary>
    public void InitBenchArgs(int botCount, bool autoBenchmark, BenchRunOptions options)
    {
        _initialBotCount = botCount;
        _autoEnabled = autoBenchmark;
        _options = options;
        // The repetition offsets the seed: same repetition on both variants gets the same pile
        // formation, different repetitions get genuinely different ones.
        _rng.Seed = (ulong)(BenchSpecs.RandomSeed + options.Repetition);
    }

    public override void _Ready()
    {
        Di.Process(this);

        if (_options.Throughput)
        {
            // A headless display server cannot draw, so each frame is padded to the low-processor sleep
            // (~6.9 ms), capping the loop at ~145 frames/s; throughput mode must run unthrottled.
            OS.LowProcessorUsageModeSleepUsec = 0;
        }
        _metrics = new BenchmarkMetrics(_options.Throughput);

        _variant = DiscoverVariant();
        if (_variant == null)
        {
            _log.Error(
                "No IPhysicsVariant implementation found in the assembly — this branch must add its variant code");
            Callable.From(() => GetTree().Quit(1)).CallDeferred();
            return;
        }

        if (_autoEnabled)
        {
            string suiteName = _options.Suite ?? BenchSuites.DefaultSuite;
            if (!BenchSuites.TryResolve(suiteName, out BenchConfig[] configs))
            {
                _log.Error("Unknown --bench-suite {suite}; known suites: {suites}",
                    suiteName, string.Join(", ", BenchSuites.Names));
                Callable.From(() => GetTree().Quit(1)).CallDeferred();
                return;
            }
            _auto = new AutoBenchmark(this, suiteName, configs, _options);
        }

        BuildTree();

        _metrics.WindowCompletedEvent += OnWindowCompleted;
        _auto?.Start();
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

        Vector2 playerCommand = _auto != null ? AutoPlayerCommand(dt) : ReadKeyboardMove();
        _player.ApplyVelocity(playerCommand);
        if (_playerMotion == PlayerMotionMode.Kite && playerCommand != Vector2.Zero)
        {
            double actualSpeed = (_player.Position - _playerPreviousPosition).Length() / dt;
            _metrics.AddPlayerSpeedRatio(actualSpeed / BenchSpecs.PlayerSpeed);
        }
        _playerPreviousPosition = _player.Position;

        if (_auto != null)
        {
            AutoFacing(dt);
        }

        SteerBots();
        SeparateBots();

        float knockbackDecay = MathF.Exp(-MathF.Log(2f) * dt / BenchSpecs.KnockbackHalfLife);
        foreach (BotUnit unit in _bots)
        {
            unit.Knockback *= knockbackDecay;
            unit.Body.ApplyVelocity(unit.Intent + unit.Separation + unit.Knockback);

            Vector2 actualVelocity = unit.Body.LastDisplacement / dt;
            _metrics.AddBotMovement(actualVelocity, unit.PreviousActualVelocity, unit.MaxSpeed);
            _metrics.AddBotSpeed(actualVelocity.Length());
            unit.PreviousActualVelocity = actualVelocity;
        }

        AdvanceRuntimeWalls(dt);
        if (_auto != null)
        {
            AutoEvents(dt);
        }

        if (_projectilesEnabled)
        {
            SpawnProjectiles(dt);
        }
        _metrics.AddProjectilesAlive(_projectilesAlive);

        _sampleTickCounter++;
        if (_sampleTickCounter >= BenchSpecs.StuckSampleIntervalTicks)
        {
            _sampleTickCounter = 0;
            _metrics.AddStuckSample(CountStuckBots());
            _metrics.AddOutsideSample(CountBotsOutsideArena());
        }

        _metrics.AddTickCallbackTime((Time.GetTicksUsec() - startUsec) / 1000f);
    }

    public override void _Process(double delta)
    {
        if (_variant == null)
        {
            return;
        }

        // In auto mode the facing is scripted per tick (in headless the mouse is meaningless).
        if (_auto == null)
        {
            _playerFacing = (GetGlobalMousePosition() - _player.Position).Angle();
            _player.SetFacing(_playerFacing);
        }
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
                    CreateWallAt(GetGlobalMousePosition(), _playerFacing);
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
                    ExplodeAt(GetGlobalMousePosition());
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

    public void ApplyAutoSetup(BenchConfig config)
    {
        SetSteering(config.Steering);
        _enemiesSelfCollide = config.SelfCollisions;
        ApplySelfCollisions();
        _playerBlocksEnemies = config.PlayerBlocks;
        ApplyPlayerBlocking();
        _hurtboxesEnabled = config.Hurtboxes;
        ApplyHurtboxes();
        _projectilesEnabled = config.ProjectileRate > 0;
        _projectileRate = config.ProjectileRate > 0 ? config.ProjectileRate : BenchSpecs.DefaultProjectileRate;

        // Every configuration starts clean: a configuration that began inside the previous one's pile
        // would measure the pile, not the configuration.
        ResetWorldState();

        _playerMotion = config.PlayerMotion;
        _facingMode = config.Facing;
        _explosions = config.Explosions;
        _explosionTimer = BenchSpecs.ExplosionPeriod;
        _wallDropPeriod = config.WallDropPeriod;
        _wallDropTimer = config.WallDropPeriod;
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
        _player.SetContinuousCollision(_options.Ccd);
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

    private Vector2 ReadKeyboardMove()
    {
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
        return moveInput.LimitLength(1f) * BenchSpecs.PlayerSpeed;
    }

    /// <summary>
    /// The kite: a carrot point walks the ellipse at the player speed, but only advances while the
    /// player is close behind — a blocked player is not dragged through the crowd, which is what makes
    /// player_speed_ratio meaningful.
    /// </summary>
    private Vector2 AutoPlayerCommand(float dt)
    {
        if (_playerMotion != PlayerMotionMode.Kite)
        {
            return Vector2.Zero;
        }

        Vector2 carrot = KiteCarrot();
        int guard = 0;
        while ((_player.Position - carrot).Length() < BenchSpecs.KiteCarrotDistance && guard++ < 64)
        {
            // Advance by the player's per-tick arc length; the local rate of the ellipse
            // parametrization turns it into a step of the angle.
            float localRate = MathF.Sqrt(
                BenchSpecs.KiteRadii.X * BenchSpecs.KiteRadii.X * MathF.Sin(_kiteAngle) * MathF.Sin(_kiteAngle)
                + BenchSpecs.KiteRadii.Y * BenchSpecs.KiteRadii.Y * MathF.Cos(_kiteAngle) * MathF.Cos(_kiteAngle));
            _kiteAngle = (_kiteAngle + BenchSpecs.PlayerSpeed * dt / localRate) % MathF.Tau;
            carrot = KiteCarrot();
        }

        Vector2 toCarrot = carrot - _player.Position;
        return toCarrot.LengthSquared() > 1f ? toCarrot.Normalized() * BenchSpecs.PlayerSpeed : Vector2.Zero;
    }

    private Vector2 KiteCarrot() => BenchSpecs.KiteCentre
        + new Vector2(
            MathF.Cos(_kiteAngle) * BenchSpecs.KiteRadii.X,
            MathF.Sin(_kiteAngle) * BenchSpecs.KiteRadii.Y);

    private void AutoFacing(float dt)
    {
        switch (_facingMode)
        {
            case FacingMode.Spin:
                _playerFacing += MathF.Tau / 4f * dt;
                break;
            case FacingMode.TowardCrowd when _bots.Count > 0:
                Vector2 centroid = Vector2.Zero;
                foreach (BotUnit unit in _bots)
                {
                    centroid += unit.Body.Position;
                }
                _playerFacing = (centroid / _bots.Count - _player.Position).Angle();
                break;
        }
        _player.SetFacing(_playerFacing);
    }

    private void AutoEvents(float dt)
    {
        if (_explosions != ExplosionMode.None)
        {
            _explosionTimer -= dt;
            if (_explosionTimer <= 0)
            {
                _explosionTimer += BenchSpecs.ExplosionPeriod;
                if (_explosions == ExplosionMode.AtPlayer)
                {
                    ExplodeAt(_player.Position);
                }
                else
                {
                    ExplodeIntoWall();
                }
            }
        }

        if (_wallDropPeriod > 0)
        {
            _wallDropTimer -= dt;
            if (_wallDropTimer <= 0)
            {
                _wallDropTimer += _wallDropPeriod;
                DropWallOnCrowd();
            }
            for (int i = _runtimeWalls.Count - 1; i >= 0; i--)
            {
                if (_runtimeWalls[i].Age >= BenchSpecs.WallDropLifetime)
                {
                    _runtimeWalls[i].Wall.QueueFree();
                    _runtimeWalls.RemoveAt(i);
                }
            }
        }
    }

    /// <summary>The IntoWall explosion: throw the crowd pressing the long wall's south face north into it.</summary>
    private void ExplodeIntoWall()
    {
        Vector2 centroid = Vector2.Zero;
        int pressing = 0;
        foreach (BotUnit unit in _bots)
        {
            Vector2 position = unit.Body.Position;
            if (position.X >= BenchSpecs.IntoWallBandMin.X && position.X <= BenchSpecs.IntoWallBandMax.X
                && position.Y >= BenchSpecs.IntoWallBandMin.Y && position.Y <= BenchSpecs.IntoWallBandMax.Y)
            {
                centroid += position;
                pressing++;
            }
        }

        if (pressing < BenchSpecs.IntoWallMinBots)
        {
            return;
        }

        ExplodeAt(centroid / pressing + new Vector2(0f, BenchSpecs.IntoWallBlastOffset));
    }

    private void DropWallOnCrowd()
    {
        Vector2 centroid = Vector2.Zero;
        int near = 0;
        foreach (BotUnit unit in _bots)
        {
            if (unit.Body.Position.DistanceSquaredTo(_player.Position)
                <= BenchSpecs.WallDropCrowdRadius * BenchSpecs.WallDropCrowdRadius)
            {
                centroid += unit.Body.Position;
                near++;
            }
        }

        if (near == 0)
        {
            return;
        }

        _wallDropHorizontal = !_wallDropHorizontal;
        CreateWallAt(centroid / near, _wallDropHorizontal ? 0f : MathF.PI / 2f);
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

        double tickOverlapSum = 0;
        int tickOverlapPairs = 0;
        float tickOverlapMax = 0;

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
                if (overlap <= 0f)
                {
                    continue;
                }

                // Every pair shows up from both sides; count it once for the penetration metrics.
                if (neighbourIndex > i)
                {
                    tickOverlapSum += overlap;
                    tickOverlapPairs++;
                    if (overlap > tickOverlapMax)
                    {
                        tickOverlapMax = overlap;
                    }
                }

                push += offset / distance
                        * (BenchSpecs.SeparationStrength * overlap / range);
            }

            unit.Separation = push.LimitLength(BenchSpecs.SeparationClamp);
        }

        if (tickOverlapPairs > 0)
        {
            _metrics.AddTickOverlaps(tickOverlapSum / tickOverlapPairs, tickOverlapMax);
        }

        float playerRange = BenchSpecs.PlayerRadius + BenchSpecs.BotRadius;
        _grid.CollectNeighbours(_player.Position, playerRange, _neighbours);
        foreach (int neighbourIndex in _neighbours)
        {
            float penetration = playerRange - _player.Position.DistanceTo(_bots[neighbourIndex].Body.Position);
            if (penetration > 0f)
            {
                _metrics.AddPlayerOverlap(penetration);
            }
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
        body.SetContinuousCollision(_options.Ccd);
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

    private void CreateWallAt(Vector2 position, float rotation)
    {
        BenchWall wall = new()
        {
            Position = position,
            Rotation = rotation,
        };
        _runtimeWallsContainer.AddChild(wall);
        wall.ResetPhysicsInterpolation();
        _runtimeWalls.Add(new RuntimeWall { Wall = wall });
    }

    private void RemoveLastRuntimeWall()
    {
        if (_runtimeWalls.Count == 0)
        {
            return;
        }

        RuntimeWall runtime = _runtimeWalls[^1];
        _runtimeWalls.RemoveAt(_runtimeWalls.Count - 1);
        runtime.Wall.QueueFree();
    }

    private void AdvanceRuntimeWalls(float dt)
    {
        foreach (RuntimeWall runtime in _runtimeWalls)
        {
            runtime.Age += dt;
        }
    }

    private void ResetWorldState()
    {
        ResetBots();
        _player.PlaceAt(Vector2.Zero);
        _playerPreviousPosition = Vector2.Zero;
        _kiteAngle = 0f;
        _projectileSpawnRemainder = 0f;
        ClearProjectiles();
        foreach (RuntimeWall runtime in _runtimeWalls)
        {
            runtime.Wall.QueueFree();
        }
        _runtimeWalls.Clear();
    }

    private void ClearProjectiles()
    {
        foreach (Node child in _projectilesContainer.GetChildren())
        {
            if (child is BenchProjectile projectile)
            {
                projectile.Destroy();
            }
        }
        _projectilesAlive = 0;
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

    private void ExplodeAt(Vector2 center)
    {
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

    /// <summary>A bot is stuck when its centre is inside static geometry or an aged runtime wall.</summary>
    private bool IsInsideWallGeometry(Vector2 position)
    {
        foreach (Rect2 rect in _arena.Rects)
        {
            if (rect.HasPoint(position))
            {
                return true;
            }
        }

        foreach (RuntimeWall runtime in _runtimeWalls)
        {
            if (runtime.Age < BenchSpecs.StuckWallMinAge)
            {
                continue;
            }

            Vector2 local = runtime.Wall.ToLocal(position);
            if (MathF.Abs(local.X) <= BenchWall.HalfLength && MathF.Abs(local.Y) <= BenchWall.HalfThickness)
            {
                return true;
            }
        }
        return false;
    }

    private int CountStuckBots()
    {
        int stuck = 0;
        foreach (BotUnit unit in _bots)
        {
            if (IsInsideWallGeometry(unit.Body.Position))
            {
                stuck++;
            }
        }
        return stuck;
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

        string auto = _auto == null
            ? string.Empty
            : $" | suite {_options.Suite ?? BenchSuites.DefaultSuite} r{_options.Repetition}"
              + $"{(_options.Throughput ? " | throughput" : "")}{(_options.Ccd ? " | ccd" : "")}";

        return $"{_variant.Title} | bots {_bots.Count} | projectiles {_projectilesAlive}"
               + $" | walls {_runtimeWalls.Count}\n"
               + $"steer {_steering} | B {_playerBlocksEnemies} | C {_enemiesSelfCollide}"
               + $" | P {_projectilesEnabled} ({_projectileRate}/s) | H {_hurtboxesEnabled}"
               + $" | auto {_autoEnabled}{auto}\n"
               + metrics;
    }

    /// <summary>A runtime wall plus its age, for the stuck metric and the auto wall-drop lifetime.</summary>
    private sealed class RuntimeWall
    {
        public required BenchWall Wall { get; init; }
        public double Age { get; set; }
    }
}
