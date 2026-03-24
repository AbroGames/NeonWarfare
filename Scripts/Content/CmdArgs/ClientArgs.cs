namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct ClientArgs(
    CommonArgs CommonArgs,
    bool AutoStart,
    bool AutoConnect,
    string AutoConnectIp,
    int? AutoConnectPort,
    string Nick)
{
    public static readonly string AutoStartFlag = "--auto-start";
    public static readonly string AutoConnectFlag = "--auto-connect";
    public static readonly string AutoConnectIpFlag = "--auto-connect-ip";
    public static readonly string AutoConnectPortFlag = "--auto-connect-port";
    public static readonly string NickFlag = "--nick";
    
    public static ClientArgs GetFromCmd(KludgeBox.Core.CmdArgsService argsService)
    {
        return new ClientArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.ContainsInCmdArgs(AutoStartFlag),
            argsService.ContainsInCmdArgs(AutoConnectFlag),
            argsService.GetStringFromCmdArgs(AutoConnectIpFlag),
            argsService.GetIntFromCmdArgs(AutoConnectPortFlag),
            argsService.GetStringFromCmdArgs(NickFlag)
        );
    }
}
