using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct PhysicsBenchmarkArgs(int? BenchCount, bool BenchAuto)
{
    public static readonly string PhysicsBenchmarkFlag = "--physics-benchmark";
    public static readonly string BenchCountParam = "--bench-count";
    public static readonly string BenchAutoFlag = "--bench-auto";

    public static PhysicsBenchmarkArgs GetFromCmd(CmdArgsService argsService)
    {
        return new PhysicsBenchmarkArgs(
            argsService.GetIntFromCmdArgs(BenchCountParam),
            argsService.ContainsInCmdArgs(BenchAutoFlag)
        );
    }
}
