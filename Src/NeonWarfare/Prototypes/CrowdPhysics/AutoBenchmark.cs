using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The automatic benchmark: walks four configurations, ramps the bot count 50 to 500 in steps of 50,
/// and at each step settles for 2 s and then collects 5 whole one-second metric windows. One CSV row per
/// step; at the end the CSV is written to user:// and the game quits. Driven entirely from process
/// frames and completed metric windows, so it also runs under --headless where no input exists.
/// </summary>
public sealed class AutoBenchmark
{
    private sealed record BenchConfig(
        string Name, SteeringMode Steering, bool SelfCollisions, bool Projectiles, bool Hurtboxes);

    private static readonly BenchConfig[] Configs =
    [
        new("chase+separation", SteeringMode.Chase, false, false, false),
        new("wall+separation", SteeringMode.Wall, false, false, false),
        new("chase+self-collisions", SteeringMode.Chase, true, false, false),
        new("chase+projectiles+hurtboxes", SteeringMode.Chase, false, true, true),
    ];

    private const int CountFrom = 50;
    private const int CountTo = 500;
    private const int CountStep = 50;
    private const double SettleSeconds = 2.0;
    private const int WindowsToCollect = 5;

    private enum Phase
    {
        Settle,
        Collect,
        Done,
    }

    private readonly PhysicsBenchmarkRoot _root;
    private readonly List<MetricsWindow> _windows = [];
    private readonly List<string> _rows = [];

    private Phase _phase = Phase.Settle;
    private double _settleTimer;
    private int _configIndex;
    private int _countIndex;

    public AutoBenchmark(PhysicsBenchmarkRoot root)
    {
        _root = root;
    }

    public void Start()
    {
        BeginStep();
    }

    public void OnFrame(double delta)
    {
        if (_phase != Phase.Settle)
        {
            return;
        }

        _settleTimer -= delta;
        if (_settleTimer <= 0)
        {
            _phase = Phase.Collect;
        }
    }

    public void OnWindow(MetricsWindow window)
    {
        if (_phase != Phase.Collect)
        {
            return;
        }

        _windows.Add(window);
        if (_windows.Count < WindowsToCollect)
        {
            return;
        }

        RecordRow();
        Advance();
    }

    private void BeginStep()
    {
        BenchConfig config = Configs[_configIndex];
        _root.ApplyAutoSetup(config.Steering, config.SelfCollisions, config.Projectiles, config.Hurtboxes);
        _root.SetBotCount(CountFrom + _countIndex * CountStep);
        _windows.Clear();
        _root.ResetMetricAccumulators();
        _phase = Phase.Settle;
        _settleTimer = SettleSeconds;
    }

    private void Advance()
    {
        _countIndex++;
        if (CountFrom + _countIndex * CountStep > CountTo)
        {
            _countIndex = 0;
            _configIndex++;
        }

        if (_configIndex >= Configs.Length)
        {
            Finish();
            return;
        }

        BeginStep();
    }

    private void RecordRow()
    {
        MetricsWindow total = Aggregate(_windows);
        BenchConfig config = Configs[_configIndex];
        int count = CountFrom + _countIndex * CountStep;

        _rows.Add(string.Create(CultureInfo.InvariantCulture,
            $"{_root.Variant.Title},{config.Name},{count},{Math.Round(total.ProjectilesAvg)},"
            + $"{total.Tps:F2},{total.EnginePhysicsMsAvg:F3},{total.EnginePhysicsMsP95:F3},"
            + $"{total.EnginePhysicsMsMax:F3},{total.Fps:F1},{total.JitterIndex:F5},"
            + $"{total.FlipShare:F4},{total.CollisionPairsAvg:F0}"));
    }

    /// <summary>Averages the collected windows weighted by their duration, and takes maxes as maxes.</summary>
    private static MetricsWindow Aggregate(List<MetricsWindow> windows)
    {
        double duration = 0;
        double tps = 0;
        double fps = 0;
        int framesAtCap = 0;
        double engineAvg = 0;
        double engineP95 = 0;
        double engineMax = 0;
        double callbackAvg = 0;
        double callbackP95 = 0;
        double callbackMax = 0;
        double jitter = 0;
        double flips = 0;
        double projectiles = 0;
        double objects = 0;
        double pairs = 0;
        double islands = 0;

        foreach (MetricsWindow window in windows)
        {
            duration += window.DurationSeconds;
            tps += window.Tps * window.DurationSeconds;
            fps += window.Fps * window.DurationSeconds;
            framesAtCap += window.FramesAtCap;
            engineAvg += window.EnginePhysicsMsAvg * window.DurationSeconds;
            engineP95 += window.EnginePhysicsMsP95 * window.DurationSeconds;
            engineMax = Math.Max(engineMax, window.EnginePhysicsMsMax);
            callbackAvg += window.TickCallbackMsAvg * window.DurationSeconds;
            callbackP95 += window.TickCallbackMsP95 * window.DurationSeconds;
            callbackMax = Math.Max(callbackMax, window.TickCallbackMsMax);
            jitter += window.JitterIndex * window.DurationSeconds;
            flips += window.FlipShare * window.DurationSeconds;
            projectiles += window.ProjectilesAvg * window.DurationSeconds;
            objects += window.ActiveObjectsAvg * window.DurationSeconds;
            pairs += window.CollisionPairsAvg * window.DurationSeconds;
            islands += window.IslandsAvg * window.DurationSeconds;
        }

        return new MetricsWindow(
            DurationSeconds: duration,
            Tps: tps / duration,
            Fps: fps / duration,
            FramesAtCap: framesAtCap,
            EnginePhysicsMsAvg: engineAvg / duration,
            EnginePhysicsMsP95: engineP95 / duration,
            EnginePhysicsMsMax: engineMax,
            TickCallbackMsAvg: callbackAvg / duration,
            TickCallbackMsP95: callbackP95 / duration,
            TickCallbackMsMax: callbackMax,
            JitterIndex: jitter / duration,
            FlipShare: flips / duration,
            ProjectilesAvg: projectiles / duration,
            ActiveObjectsAvg: objects / duration,
            CollisionPairsAvg: pairs / duration,
            IslandsAvg: islands / duration);
    }

    private void Finish()
    {
        _phase = Phase.Done;
        string path = $"user://physics-benchmark-{_root.Variant.FileStamp}-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";

        using FileAccess file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
        file.StoreLine(
            "variant,configuration,bots,projectiles,avg_tps,tick_ms_avg,tick_ms_p95,tick_ms_max," +
            "avg_fps,jitter_index,flip_share,collision_pairs");
        foreach (string row in _rows)
        {
            file.StoreLine(row);
        }

        _root.FinishBenchmark(ProjectSettings.GlobalizePath(path));
    }
}
