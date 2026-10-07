namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// Everything measured over one one-second window. All the rate values come from wall-clock time; the
/// two kinds of physics tick time are kept apart: the engine monitor value (what
/// Performance.Monitor.TimePhysicsProcess reports) and the harness's own wall time around the tick work.
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
    double IslandsAvg);
