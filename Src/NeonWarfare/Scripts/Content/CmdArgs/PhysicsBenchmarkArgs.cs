using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct PhysicsBenchmarkArgs(
    int? BenchCount,
    bool BenchAuto,
    string BenchSuite,
    string BenchOut,
    int? BenchRep,
    bool BenchThroughput,
    bool BenchCcd)
{
    public static readonly string PhysicsBenchmarkFlag = "--physics-benchmark";
    public static readonly string BenchCountParam = "--bench-count";
    public static readonly string BenchAutoFlag = "--bench-auto";
    public static readonly string BenchSuiteParam = "--bench-suite";
    public static readonly string BenchOutParam = "--bench-out";
    public static readonly string BenchRepParam = "--bench-rep";
    public static readonly string BenchThroughputFlag = "--bench-throughput";
    public static readonly string BenchCcdFlag = "--bench-ccd";

    public static PhysicsBenchmarkArgs GetFromCmd(CmdArgsService argsService)
    {
        return new PhysicsBenchmarkArgs(
            argsService.GetIntFromCmdArgs(BenchCountParam),
            argsService.ContainsInCmdArgs(BenchAutoFlag),
            argsService.GetStringFromCmdArgs(BenchSuiteParam),
            argsService.GetStringFromCmdArgs(BenchOutParam),
            argsService.GetIntFromCmdArgs(BenchRepParam),
            argsService.ContainsInCmdArgs(BenchThroughputFlag),
            argsService.ContainsInCmdArgs(BenchCcdFlag)
        );
    }
}
