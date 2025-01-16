namespace NeonWarfare.Scripts.Utils.CmdArgs;

public readonly record struct ClientParams(string AutoConnectIp, int? AutoConnectPort)
{
    public static readonly string AutoConnectIpFlag = "--auto-connect-ip";
    public static readonly string AutoConnectPortFlag = "--auto-connect-port";
    
    public static ClientParams GetFromCmd()
    {
        return new ClientParams(
            CmdArgsService.GetStringFromCmdArgs(AutoConnectIpFlag),
            CmdArgsService.GetIntFromCmdArgs(AutoConnectPortFlag)
        );
    }
}
