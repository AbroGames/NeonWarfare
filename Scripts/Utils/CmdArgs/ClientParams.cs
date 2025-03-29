namespace NeonWarfare.Scripts.Utils.CmdArgs;

public readonly record struct ClientParams(string AutoConnectIp, int? AutoConnectPort, bool RunProfiler, bool IsHelper)
{
    public static readonly string AutoConnectIpFlag = "--auto-connect-ip";
    public static readonly string AutoConnectPortFlag = "--auto-connect-port";
    public static readonly string RunProfilerFlag = "--run-profiler";
    public static readonly string RunAsHelperFlag = "--helper";
    
    public static ClientParams GetFromCmd()
    {
        return new ClientParams(
            CmdArgsService.GetStringFromCmdArgs(AutoConnectIpFlag),
            CmdArgsService.GetIntFromCmdArgs(AutoConnectPortFlag),
            CmdArgsService.ContainsInCmdArgs(RunProfilerFlag),
            CmdArgsService.ContainsInCmdArgs(RunAsHelperFlag)
        );
    }
}
