namespace NeonWarfare.Prototypes.CrowdPhysics;

/// <summary>
/// The run-wide options of the automatic benchmark, mapped from the command line by the root starter.
/// One CSV per run; the repetition offsets the random seed so repetitions differ in pile formation,
/// not just in timing noise.
/// </summary>
public sealed record BenchRunOptions(
    string Suite,
    string OutDirectory,
    int Repetition,
    bool Throughput,
    bool Ccd);
