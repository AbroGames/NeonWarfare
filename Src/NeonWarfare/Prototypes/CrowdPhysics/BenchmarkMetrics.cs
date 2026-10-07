using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Collects the per-tick and per-frame samples and closes them into one MetricsWindow per wall-clock
/// second. The automatic benchmark listens for completed windows, so its numbers are always whole
/// windows and comparable between the variants.
/// </summary>
public class BenchmarkMetrics
{
    private const long WindowUsec = 1_000_000;

    /// <summary>Bots slower than this are not moving; their samples would only dilute the jitter metric.</summary>
    private const float MovementNoiseThreshold = 5f;

    /// <summary>Cosine of 120 degrees: a direction change past it counts as a flip.</summary>
    private const double FlipCosine = -0.5;

    private readonly List<double> _enginePhysicsMs = new(128);
    private readonly List<double> _tickCallbackMs = new(128);

    private ulong _windowStartUsec;
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

    public MetricsWindow? LastWindow { get; private set; }

    /// <summary>Raised once per completed one-second window, on a process frame.</summary>
    public event Action<MetricsWindow> WindowCompletedEvent;

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

    public void AddProjectilesAlive(int alive)
    {
        _projectileSum += alive;
        _projectileSamples++;
    }

    /// <summary>
    /// Called once per rendered frame: fps, monitor samples and the catch-up-cap counter. The engine
    /// monitor reports seconds and holds the running max physics-iteration time of the current
    /// wall-clock second; store milliseconds so the window statistics are directly readable.
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
        if (_windowStartUsec == 0)
        {
            _windowStartUsec = now;
            return;
        }
        if (now - _windowStartUsec >= WindowUsec)
        {
            CloseWindow(now);
        }
    }

    /// <summary>Drops the in-flight samples so the next completed window measures only what comes after.</summary>
    public void ResetAccumulators()
    {
        _windowStartUsec = Time.GetTicksUsec();
        _enginePhysicsMs.Clear();
        _tickCallbackMs.Clear();
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
    }

    private void CloseWindow(ulong now)
    {
        double seconds = (now - _windowStartUsec) / 1_000_000.0;

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
            IslandsAvg: _monitorSamples > 0 ? _islandsSum / _monitorSamples : 0);

        ResetAccumulators();
        WindowCompletedEvent?.Invoke(LastWindow.Value);
    }
}

/// <summary>Percentile over a sorted copy, nearest-rank: good enough for one-second sample sets.</summary>
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
        int index = Math.Max(0, Math.Min(sorted.Count - 1, (int)Math.Ceiling(percentile * sorted.Count) - 1));
        return sorted[index];
    }
}
