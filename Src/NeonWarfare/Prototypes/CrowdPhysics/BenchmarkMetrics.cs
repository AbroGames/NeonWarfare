using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Collects the per-tick and per-frame samples and closes them into one MetricsWindow — every
/// wall-clock second in realtime mode, every ThroughputWindowTicks physics ticks in throughput mode
/// (with --fixed-fps 60 that is one second of game time per window, and the window's wall time per
/// tick is the full cost of a tick: callbacks, the physics server step and the near-zero headless
/// render in one number). The automatic benchmark listens for completed windows, so its numbers are
/// always whole windows and comparable between the variants.
/// </summary>
public class BenchmarkMetrics
{
    private const long WindowUsec = 1_000_000;

    /// <summary>Bots slower than this are not moving; their samples would only dilute the jitter metric.</summary>
    private const float MovementNoiseThreshold = 5f;

    /// <summary>Cosine of 120 degrees: a direction change past it counts as a flip.</summary>
    private const double FlipCosine = -0.5;

    private readonly bool _throughput;

    private readonly List<double> _enginePhysicsMs = new(128);
    private readonly List<double> _tickCallbackMs = new(128);
    private readonly List<double> _frameMs = new(128);

    private ulong _windowStartUsec;
    private ulong _frameLastUsec;
    private int _ticks;
    private int _ticksSinceFrame;
    private int _frames;
    private int _framesAtCap;
    private double _jitterSum;
    private int _jitterSamples;
    private int _flips;
    private int _flipSamples;
    private double _projectileSum;
    private int _projectileSamples;
    private double _activeObjectsSum;
    private double _collisionPairsSum;
    private double _islandsSum;
    private int _monitorSamples;
    private double _overlapMeanSum;
    private int _overlapPairTicks;
    private double _overlapMaxPx;
    private double _playerOverlapMaxPx;
    private int _stuckMax;
    private int _outsideMax;
    private double _maxBotSpeed;
    private int _flung;
    private double _playerSpeedRatioSum;
    private int _playerSpeedRatioSamples;

    public MetricsWindow? LastWindow { get; private set; }

    /// <summary>Raised once per completed window, on a process frame.</summary>
    public event Action<MetricsWindow> WindowCompletedEvent;

    public BenchmarkMetrics(bool throughput)
    {
        _throughput = throughput;
    }

    public void OnTickStart()
    {
        _ticks++;
        _ticksSinceFrame++;
    }

    public void AddTickCallbackTime(float milliseconds)
    {
        _tickCallbackMs.Add(milliseconds);
    }

    public void AddBotMovement(Vector2 velocity, Vector2 previousVelocity, float maxSpeed)
    {
        if (MathF.Max(velocity.Length(), previousVelocity.Length()) <= MovementNoiseThreshold)
        {
            return;
        }

        _jitterSum += (velocity - previousVelocity).Length() / maxSpeed;
        _jitterSamples++;

        double dot = (double)velocity.X * previousVelocity.X + (double)velocity.Y * previousVelocity.Y;
        if (dot / (velocity.Length() * previousVelocity.Length()) < FlipCosine)
        {
            _flips++;
        }
        _flipSamples++;
    }

    /// <summary>The bot's actual displacement speed this tick, the source of max_bot_speed and `flung`.</summary>
    public void AddBotSpeed(float speed)
    {
        if (speed > _maxBotSpeed)
        {
            _maxBotSpeed = speed;
        }
        if (speed > BenchSpecs.FlungSpeedThreshold)
        {
            _flung++;
        }
    }

    /// <summary>
    /// One tick's bot-bot penetration: the mean over overlapping pairs of that tick and its max. Ticks
    /// without overlapping pairs add nothing, so the window mean stays a mean over overlapping pairs.
    /// </summary>
    public void AddTickOverlaps(double meanPx, double maxPx)
    {
        _overlapMeanSum += meanPx;
        _overlapPairTicks++;
        if (maxPx > _overlapMaxPx)
        {
            _overlapMaxPx = maxPx;
        }
    }

    public void AddPlayerOverlap(float penetrationPx)
    {
        if (penetrationPx > _playerOverlapMaxPx)
        {
            _playerOverlapMaxPx = penetrationPx;
        }
    }

    public void AddStuckSample(int stuckBots)
    {
        if (stuckBots > _stuckMax)
        {
            _stuckMax = stuckBots;
        }
    }

    public void AddOutsideSample(int outsideBots)
    {
        if (outsideBots > _outsideMax)
        {
            _outsideMax = outsideBots;
        }
    }

    /// <summary>Actual player speed over commanded speed, one sample per tick with a nonzero command.</summary>
    public void AddPlayerSpeedRatio(double ratio)
    {
        _playerSpeedRatioSum += ratio;
        _playerSpeedRatioSamples++;
    }

    public void AddProjectilesAlive(int alive)
    {
        _projectileSum += alive;
        _projectileSamples++;
    }

    /// <summary>
    /// Called once per rendered frame: fps, monitor samples, the frame-to-frame wall time and the
    /// catch-up-cap counter. The engine monitor reports seconds and holds the running max
    /// physics-iteration time of the current wall-clock second; store milliseconds so the window
    /// statistics are directly readable.
    /// </summary>
    public void OnFrame()
    {
        _frames++;

        if (_ticksSinceFrame >= Engine.MaxPhysicsStepsPerFrame)
        {
            _framesAtCap++;
        }
        _ticksSinceFrame = 0;

        _enginePhysicsMs.Add(Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0);
        _activeObjectsSum += Performance.GetMonitor(Performance.Monitor.Physics2DActiveObjects);
        _collisionPairsSum += Performance.GetMonitor(Performance.Monitor.Physics2DCollisionPairs);
        _islandsSum += Performance.GetMonitor(Performance.Monitor.Physics2DIslandCount);
        _monitorSamples++;

        ulong now = Time.GetTicksUsec();
        if (_frameLastUsec != 0)
        {
            _frameMs.Add((now - _frameLastUsec) / 1000.0);
        }
        _frameLastUsec = now;

        if (_windowStartUsec == 0)
        {
            _windowStartUsec = now;
            return;
        }
        bool windowOver = _throughput
            ? _ticks >= BenchSpecs.ThroughputWindowTicks
            : now - _windowStartUsec >= WindowUsec;
        if (windowOver)
        {
            CloseWindow(now);
        }
    }

    /// <summary>Drops the in-flight samples so the next completed window measures only what comes after.</summary>
    public void ResetAccumulators()
    {
        _windowStartUsec = Time.GetTicksUsec();
        _frameLastUsec = 0;
        _enginePhysicsMs.Clear();
        _tickCallbackMs.Clear();
        _frameMs.Clear();
        _ticks = 0;
        _ticksSinceFrame = 0;
        _frames = 0;
        _framesAtCap = 0;
        _jitterSum = 0;
        _jitterSamples = 0;
        _flips = 0;
        _flipSamples = 0;
        _projectileSum = 0;
        _projectileSamples = 0;
        _activeObjectsSum = 0;
        _collisionPairsSum = 0;
        _islandsSum = 0;
        _monitorSamples = 0;
        _overlapMeanSum = 0;
        _overlapPairTicks = 0;
        _overlapMaxPx = 0;
        _playerOverlapMaxPx = 0;
        _stuckMax = 0;
        _outsideMax = 0;
        _maxBotSpeed = 0;
        _flung = 0;
        _playerSpeedRatioSum = 0;
        _playerSpeedRatioSamples = 0;
    }

    private void CloseWindow(ulong now)
    {
        double seconds = Math.Max((now - _windowStartUsec) / 1_000_000.0, 1e-6);

        LastWindow = new MetricsWindow(
            DurationSeconds: seconds,
            Tps: _ticks / seconds,
            Fps: _frames / seconds,
            FramesAtCap: _framesAtCap,
            EnginePhysicsMsAvg: _enginePhysicsMs.AverageOrDefault(),
            EnginePhysicsMsP95: _enginePhysicsMs.Percentile(0.95),
            EnginePhysicsMsMax: _enginePhysicsMs.DefaultIfEmpty(0).Max(),
            TickCallbackMsAvg: _tickCallbackMs.AverageOrDefault(),
            TickCallbackMsP95: _tickCallbackMs.Percentile(0.95),
            TickCallbackMsMax: _tickCallbackMs.DefaultIfEmpty(0).Max(),
            JitterIndex: _jitterSamples > 0 ? _jitterSum / _jitterSamples : 0,
            FlipShare: _flipSamples > 0 ? (double)_flips / _flipSamples : 0,
            ProjectilesAvg: _projectileSamples > 0 ? _projectileSum / _projectileSamples : 0,
            ActiveObjectsAvg: _monitorSamples > 0 ? _activeObjectsSum / _monitorSamples : 0,
            CollisionPairsAvg: _monitorSamples > 0 ? _collisionPairsSum / _monitorSamples : 0,
            IslandsAvg: _monitorSamples > 0 ? _islandsSum / _monitorSamples : 0,
            FrameMsP50: _frameMs.Percentile(0.50),
            FrameMsP99: _frameMs.Percentile(0.99),
            FrameMsMax: _frameMs.DefaultIfEmpty(0).Max(),
            FrameMsSamples: [.. _frameMs],
            OverlapMeanSum: _overlapMeanSum,
            OverlapPairTicks: _overlapPairTicks,
            OverlapMaxPx: _overlapMaxPx,
            PlayerOverlapMaxPx: _playerOverlapMaxPx,
            StuckMax: _stuckMax,
            OutsideMax: _outsideMax,
            MaxBotSpeed: _maxBotSpeed,
            FlungCount: _flung,
            PlayerSpeedRatioSum: _playerSpeedRatioSum,
            PlayerSpeedRatioSamples: _playerSpeedRatioSamples);

        ResetAccumulators();
        WindowCompletedEvent?.Invoke(LastWindow.Value);
    }
}

/// <summary>Percentile over a sorted copy, nearest-rank: good enough for a one-window sample set.</summary>
public static class MetricsMathExtensions
{
    public static double AverageOrDefault(this IEnumerable<double> values) =>
        values.Any() ? values.Average() : 0;

    public static double Percentile(this List<double> values, double percentile)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        List<double> sorted = [.. values];
        sorted.Sort();
        return sorted.PercentileSorted(percentile);
    }

    public static double Percentile(this double[] values, double percentile)
    {
        if (values.Length == 0)
        {
            return 0;
        }

        List<double> sorted = [.. values];
        sorted.Sort();
        return sorted.PercentileSorted(percentile);
    }

    private static double PercentileSorted(this List<double> sorted, double percentile) =>
        sorted[Math.Max(0, Math.Min(sorted.Count - 1, (int)Math.Ceiling(percentile * sorted.Count) - 1))];
}
