using NeonWarfare.Utils;

namespace NeonWarfare.Net;

public readonly record struct ClientParams(bool AutoTest, string AutoConnectIp, int? AutoConnectPort)
{
    public static readonly string AutoTestFlag = "--auto-test";
    public static readonly string AutoConnectIpFlag = "--auto-connect-ip";
    public static readonly string AutoConnectPortFlag = "--auto-connect-port";
    
    public static ClientParams GetFromCmd()
    {
        return new ClientParams(
            CmdArgsService.ContainsInCmdArgs(AutoTestFlag),
            CmdArgsService.GetStringFromCmdArgs(AutoConnectIpFlag),
            CmdArgsService.GetIntFromCmdArgs(AutoConnectPortFlag)
        );
    }
}