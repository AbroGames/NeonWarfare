using NeonWarfare.Utils;

namespace NeonWarfare;

public readonly record struct ClientParams(int? FastTest, string AutoConnectIp, int? AutoConnectPort)
{
    public static readonly string FastTestFlag = "--fast-test";
    public static readonly string AutoConnectIpFlag = "--auto-connect-ip";
    public static readonly string AutoConnectPortFlag = "--auto-connect-port";
    
    public static ClientParams GetFromCmd()
    {
        return new ClientParams(
            CmdArgsService.GetIntFromCmdArgs(FastTestFlag),
            CmdArgsService.GetStringFromCmdArgs(AutoConnectIpFlag),
            CmdArgsService.GetIntFromCmdArgs(AutoConnectPortFlag)
        );
    }
}