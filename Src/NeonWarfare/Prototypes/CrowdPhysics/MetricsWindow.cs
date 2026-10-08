namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Everything measured over one metric window: one wall-clock second in realtime mode, 60 physics
/// ticks (1 s of game time) in throughput mode. The two kinds of physics tick time are kept apart: the
/// engine monitor value (what Performance.Monitor.TimePhysicsProcess reports) and the harness's own
/// wall time around the tick work. `overlapMeanSum` / `overlapPairTicks` carry the mean over
/// overlapping pairs per tick so windows can be combined exactly.
/// </summary>
public readonly record struct MetricsWindow(
    double DurationSeconds,
    double Tps,
    double Fps,
    int FramesAtCap,
    double EnginePhysicsMsAvg,
    double EnginePhysicsMsP95,
    double EnginePhysicsMsMax,
    double TickCallbackMsAvg,
    double TickCallbackMsP95,
    double TickCallbackMsMax,
    double JitterIndex,
    double FlipShare,
    double ProjectilesAvg,
    double ActiveObjectsAvg,
    double CollisionPairsAvg,
    double IslandsAvg,
    double FrameMsP50,
    double FrameMsP99,
    double FrameMsMax,
    double[] FrameMsSamples,
    double OverlapMeanSum,
    int OverlapPairTicks,
    double OverlapMaxPx,
    double PlayerOverlapMaxPx,
    int StuckMax,
    int OutsideMax,
    double MaxBotSpeed,
    int FlungCount,
    double PlayerSpeedRatioSum,
    int PlayerSpeedRatioSamples);
