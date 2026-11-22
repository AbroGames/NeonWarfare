using System.Globalization;
using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct PhysicsBenchmarkArgs(
    int? BenchCount,
    bool BenchAuto,
    string BenchSuite,
    string BenchOut,
    int? BenchRep,
    bool BenchThroughput,
    bool BenchCcd,
    bool Demo,
    float? SeparationStrength,
    float? SeparationSmoothing,
    float? SeparationRange,
    bool SeparationAveraged,
    string CrowdDamping)
{
    public static readonly string PhysicsBenchmarkFlag = "--physics-benchmark";
    public static readonly string BenchCountParam = "--count";
    public static readonly string BenchAutoFlag = "--bench-auto";
    public static readonly string BenchSuiteParam = "--bench-suite";
    public static readonly string BenchOutParam = "--bench-out";
    public static readonly string BenchRepParam = "--bench-rep";
    public static readonly string BenchThroughputFlag = "--bench-throughput";
    public static readonly string BenchCcdFlag = "--bench-ccd";
    public static readonly string DemoFlag = "--demo";
    public static readonly string SeparationStrengthParam = "-J";
    public static readonly string SeparationSmoothingParam = "-L";
    public static readonly string SeparationRangeParam = "-O";
    public static readonly string SeparationAveragedFlag = "-N";
    public static readonly string CrowdDampingParam = "-Y";

    public static PhysicsBenchmarkArgs GetFromCmd(CmdArgsService argsService)
    {
        return new PhysicsBenchmarkArgs(
            argsService.GetIntFromCmdArgs(BenchCountParam),
            argsService.ContainsInCmdArgs(BenchAutoFlag),
            argsService.GetStringFromCmdArgs(BenchSuiteParam),
            argsService.GetStringFromCmdArgs(BenchOutParam),
            argsService.GetIntFromCmdArgs(BenchRepParam),
            argsService.ContainsInCmdArgs(BenchThroughputFlag),
            argsService.ContainsInCmdArgs(BenchCcdFlag),
            argsService.ContainsInCmdArgs(DemoFlag),
            ParseFloat(argsService.GetStringFromCmdArgs(SeparationStrengthParam)),
            ParseFloat(argsService.GetStringFromCmdArgs(SeparationSmoothingParam)),
            ParseFloat(argsService.GetStringFromCmdArgs(SeparationRangeParam)),
            argsService.ContainsInCmdArgs(SeparationAveragedFlag),
            argsService.GetStringFromCmdArgs(CrowdDampingParam)
        );
    }

    // The invariant culture keeps "0.01" valid on a system with a decimal comma.
    private static float? ParseFloat(string value) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result) ? result : null;
}
