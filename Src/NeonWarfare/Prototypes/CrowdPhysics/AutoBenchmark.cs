using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Godot;

namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The automatic benchmark: walks the configurations of one suite and ramps each configuration over
/// its bot counts. Every step settles for 2 s of game time and then collects five metric windows —
/// whole seconds in realtime mode, 60-tick chunks of game time in throughput mode, so a step always
/// simulates the same 420 ticks in both. Every configuration starts from a clean arena (the root's
/// ApplyAutoSetup resets the world), one CSV row is appended and flushed per step so a killed run
/// keeps everything measured so far. Early stop drops the remaining counts of a configuration whose
/// step became hopeless; the watchdog drops a step that stopped making progress at all.
/// </summary>
public sealed class AutoBenchmark
{
    private enum Phase
    {
        Settle,
        Collect,
        Done,
    }

    private const double SettleGameSeconds = 2.0;
    private const int WindowsToCollect = 5;
    private const long StepWatchdogUsec = (long)(BenchSpecs.StepWatchdogSeconds * 1_000_000);

    private const string CsvHeader =
        "variant,suite,config,rep,mode,mode_ok,ccd,seed,bots,projectile_rate,projectiles_alive,"
        + "tps,tick_full_ms,frame_ms_p50,frame_ms_p99,frame_ms_max,engine_max_ms,"
        + "callbacks_ms_avg,callbacks_ms_p95,callbacks_ms_max,fps,frames_at_cap,"
        + "jitter_index,flip_share,overlap_mean_px,overlap_max_px,player_overlap_max_px,player_speed_ratio,"
        + "stuck_max,outside_max,max_bot_speed,flung,"
        + "collision_pairs,active_objects,islands,gc_gen0,alloc_kb_per_tick,early_stop";

    private readonly PhysicsBenchmarkRoot _root;
    private readonly BenchConfig[] _configs;
    private readonly string _suiteName;
    private readonly int _rep;
    private readonly bool _throughput;
    private readonly bool _ccd;
    private readonly string _csvPath;

    private StreamWriter _csv;
    private Phase _phase = Phase.Settle;
    private double _settleTimer;
    private int _configIndex;
    private int _countIndex;
    private ulong _stepStartUsec;
    private bool _firstWindowSeen;
    private bool _throughputOk = true;
    private long _gcGen0AtCollectStart;
    private long _allocBytesAtCollectStart;
    private readonly List<MetricsWindow> _windows = [];
    private readonly List<double> _stepFrameMs = [];
    private MetricsWindow _lastAggregate;

    public AutoBenchmark(PhysicsBenchmarkRoot root, string suiteName, BenchConfig[] configs, BenchRunOptions options)
    {
        _root = root;
        _suiteName = suiteName;
        _configs = configs;
        _rep = options.Repetition;
        _throughput = options.Throughput;
        _ccd = options.Ccd;

        string directory = ProjectSettings.GlobalizePath(
            string.IsNullOrEmpty(options.OutDirectory) ? "user://" : options.OutDirectory);
        string ccdStamp = _ccd ? "-ccd" : string.Empty;
        string file = $"physics-benchmark-{root.Variant.FileStamp}-{_suiteName}-r{_rep}{ccdStamp}"
                      + $"-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
        _csvPath = Path.Combine(directory, file);
    }

    public void Start()
    {
        _root.Log.Information("Writing benchmark CSV to {csvPath}", _csvPath);
        _csv = new StreamWriter(_csvPath, append: false);
        _csv.WriteLine(CsvHeader);
        _csv.Flush();
        BeginStep();
    }

    public void OnFrame(double delta)
    {
        if (_phase == Phase.Done)
        {
            return;
        }

        if (Time.GetTicksUsec() - _stepStartUsec > StepWatchdogUsec)
        {
            HandleWatchdog();
            return;
        }

        if (_phase != Phase.Settle)
        {
            return;
        }

        _settleTimer -= delta;
        if (_settleTimer <= 0)
        {
            _phase = Phase.Collect;
            _gcGen0AtCollectStart = GC.CollectionCount(0);
            _allocBytesAtCollectStart = GC.GetAllocatedBytesForCurrentThread();
        }
    }

    public void OnWindow(MetricsWindow window)
    {
        if (_phase != Phase.Collect)
        {
            return;
        }

        if (_throughput && !_firstWindowSeen)
        {
            _firstWindowSeen = true;
            _throughputOk = window.Tps >= BenchSpecs.ThroughputMinTps;
            if (!_throughputOk)
            {
                _root.Log.Warning(
                    "throughput mode not effective: was --fixed-fps 60 passed? (first window TPS {tps:F1})",
                    window.Tps);
            }
        }

        _windows.Add(window);
        _stepFrameMs.AddRange(window.FrameMsSamples);
        if (_windows.Count < WindowsToCollect)
        {
            return;
        }

        RecordRow(earlyStop: 0);
        Advance();
    }

    private void BeginStep()
    {
        BenchConfig config = _configs[_configIndex];
        if (_countIndex == 0)
        {
            _root.ApplyAutoSetup(config);
        }
        _root.SetBotCount(config.Counts[_countIndex]);
        _root.ResetMetricAccumulators();
        _windows.Clear();
        _stepFrameMs.Clear();
        _phase = Phase.Settle;
        _settleTimer = SettleGameSeconds;
        _stepStartUsec = Time.GetTicksUsec();
    }

    private void Advance()
    {
        // A step past the early-stop threshold would only get slower; jump to the next configuration.
        bool earlyStop = _throughput
            ? TickFullMs(_lastAggregate) > BenchSpecs.EarlyStopTickMs
            : _lastAggregate.Tps < BenchSpecs.EarlyStopRealtimeTps;

        if (!earlyStop)
        {
            _countIndex++;
            if (_countIndex < _configs[_configIndex].Counts.Length)
            {
                BeginStep();
                return;
            }
        }

        NextConfig();
    }

    /// <summary>A step out of wall time: record what it has and skip to the next configuration.</summary>
    private void HandleWatchdog()
    {
        if (_phase == Phase.Collect && _windows.Count > 0)
        {
            RecordRow(earlyStop: 2);
        }
        else
        {
            _root.Log.Warning(
                "Watchdog: {suite}/{config} step made no complete window in {seconds:F0} s — skipping without a row",
                _suiteName, _configs[_configIndex].Name, BenchSpecs.StepWatchdogSeconds);
        }
        NextConfig();
    }

    private void NextConfig()
    {
        _configIndex++;
        _countIndex = 0;
        if (_configIndex < _configs.Length)
        {
            BeginStep();
            return;
        }

        _phase = Phase.Done;
        _csv.Dispose();
        _root.FinishBenchmark(_csvPath);
    }

    private void RecordRow(int earlyStop)
    {
        MetricsWindow total = Aggregate(_windows);
        _lastAggregate = total;
        BenchConfig config = _configs[_configIndex];
        int count = config.Counts[_countIndex];
        long seed = BenchSpecs.RandomSeed + _rep;

        string tickFull = _throughput
            ? TickFullMs(total).ToString("F3", CultureInfo.InvariantCulture)
            : string.Empty;
        string playerSpeedRatio = total.PlayerSpeedRatioSamples > 0
            ? (total.PlayerSpeedRatioSum / total.PlayerSpeedRatioSamples).ToString("F3", CultureInfo.InvariantCulture)
            : string.Empty;
        double overlapMean = total.OverlapPairTicks > 0
            ? total.OverlapMeanSum / total.OverlapPairTicks
            : 0;

        long allocated = GC.GetAllocatedBytesForCurrentThread() - _allocBytesAtCollectStart;
        string row = string.Create(CultureInfo.InvariantCulture,
            $"{_root.Variant.Title},{_suiteName},{config.Name},{_rep},{Mode()},"
            + $"{(_throughputOk ? 1 : 0)},{(_ccd ? 1 : 0)},{seed},{count},{config.ProjectileRate},"
            + $"{Math.Round(total.ProjectilesAvg)},{total.Tps:F2},{tickFull},"
            + $"{total.FrameMsP50:F3},{total.FrameMsP99:F3},{total.FrameMsMax:F3},{total.EnginePhysicsMsMax:F3},"
            + $"{total.TickCallbackMsAvg:F3},{total.TickCallbackMsP95:F3},{total.TickCallbackMsMax:F3},"
            + $"{total.Fps:F1},{total.FramesAtCap},{total.JitterIndex:F5},{total.FlipShare:F4},"
            + $"{overlapMean:F4},{total.OverlapMaxPx:F3},{total.PlayerOverlapMaxPx:F3},{playerSpeedRatio},"
            + $"{total.StuckMax},{total.OutsideMax},{total.MaxBotSpeed:F1},{total.FlungCount},"
            + $"{total.CollisionPairsAvg:F0},{total.ActiveObjectsAvg:F0},{total.IslandsAvg:F0},"
            + $"{GC.CollectionCount(0) - _gcGen0AtCollectStart},"
            + $"{allocated / (double)TotalTicks() / 1024.0:F3},"
            + $"{earlyStop}");

        _csv.WriteLine(row);
        _csv.Flush();
        _root.Log.Information("Bench row: {row}", row);
    }

    private string Mode() => _throughput ? "throughput" : "realtime";

    /// <summary>
    /// The full cost of one physics tick: the window's wall time per tick. In throughput mode every
    /// frame advances exactly one tick of game time as fast as the CPU allows, so this covers the
    /// callbacks, the physics server step and the render alike — the same quantity for both variants.
    /// </summary>
    private static double TickFullMs(MetricsWindow aggregate) => 1000.0 / Math.Max(aggregate.Tps, 1e-9);

    private int TotalTicks()
    {
        double ticks = 0;
        foreach (MetricsWindow window in _windows)
        {
            ticks += window.Tps * window.DurationSeconds;
        }
        return Math.Max((int)Math.Round(ticks), 1);
    }

    /// <summary>Averages the collected windows weighted by their duration; maxes stay maxes, sums sum.</summary>
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
        double frameP50 = 0;
        double frameP99 = 0;
        double frameMax = 0;
        double overlapMeanSum = 0;
        int overlapPairTicks = 0;
        double overlapMax = 0;
        double playerOverlapMax = 0;
        int stuckMax = 0;
        int outsideMax = 0;
        double maxBotSpeed = 0;
        int flung = 0;
        double playerSpeedRatioSum = 0;
        int playerSpeedRatioSamples = 0;
        var frameSamples = new List<double>();

        foreach (MetricsWindow window in windows)
        {
            double seconds = window.DurationSeconds;
            duration += seconds;
            tps += window.Tps * seconds;
            fps += window.Fps * seconds;
            framesAtCap += window.FramesAtCap;
            engineAvg += window.EnginePhysicsMsAvg * seconds;
            engineP95 += window.EnginePhysicsMsP95 * seconds;
            engineMax = Math.Max(engineMax, window.EnginePhysicsMsMax);
            callbackAvg += window.TickCallbackMsAvg * seconds;
            callbackP95 += window.TickCallbackMsP95 * seconds;
            callbackMax = Math.Max(callbackMax, window.TickCallbackMsMax);
            jitter += window.JitterIndex * seconds;
            flips += window.FlipShare * seconds;
            projectiles += window.ProjectilesAvg * seconds;
            objects += window.ActiveObjectsAvg * seconds;
            pairs += window.CollisionPairsAvg * seconds;
            islands += window.IslandsAvg * seconds;
            frameP50 += window.FrameMsP50 * seconds;
            frameP99 += window.FrameMsP99 * seconds;
            frameMax = Math.Max(frameMax, window.FrameMsMax);
            frameSamples.AddRange(window.FrameMsSamples);
            overlapMeanSum += window.OverlapMeanSum;
            overlapPairTicks += window.OverlapPairTicks;
            overlapMax = Math.Max(overlapMax, window.OverlapMaxPx);
            playerOverlapMax = Math.Max(playerOverlapMax, window.PlayerOverlapMaxPx);
            stuckMax = Math.Max(stuckMax, window.StuckMax);
            outsideMax = Math.Max(outsideMax, window.OutsideMax);
            maxBotSpeed = Math.Max(maxBotSpeed, window.MaxBotSpeed);
            flung += window.FlungCount;
            playerSpeedRatioSum += window.PlayerSpeedRatioSum;
            playerSpeedRatioSamples += window.PlayerSpeedRatioSamples;
        }

        var samples = frameSamples.ToArray();
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
            IslandsAvg: islands / duration,
            FrameMsP50: samples.Percentile(0.50),
            FrameMsP99: samples.Percentile(0.99),
            FrameMsMax: frameMax,
            FrameMsSamples: samples,
            OverlapMeanSum: overlapMeanSum,
            OverlapPairTicks: overlapPairTicks,
            OverlapMaxPx: overlapMax,
            PlayerOverlapMaxPx: playerOverlapMax,
            StuckMax: stuckMax,
            OutsideMax: outsideMax,
            MaxBotSpeed: maxBotSpeed,
            FlungCount: flung,
            PlayerSpeedRatioSum: playerSpeedRatioSum,
            PlayerSpeedRatioSamples: playerSpeedRatioSamples);
    }
}
