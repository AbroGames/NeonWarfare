using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct ClientArgs(
    CommonArgs CommonArgs,
    bool AutoStart,
    string AutoStartSaveFileName,
    bool AutoConnect,
    string AutoConnectIp,
    int? AutoConnectPort,
    string Nick,
    string Uid)
{
    public static readonly string AutoStartFlag = "--auto-start";
    public static readonly string AutoStartSaveFileNameParam = "--auto-start-savefile";
    public static readonly string AutoConnectFlag = "--auto-connect";
    public static readonly string AutoConnectIpParam = "--auto-connect-ip";
    public static readonly string AutoConnectPortParam = "--auto-connect-port";
    public static readonly string NickParam = "--nick";
    public static readonly string UidParam = "--uid";
    
    public static ClientArgs GetFromCmd(CmdArgsService argsService)
    {
        return new ClientArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.ContainsInCmdArgs(AutoStartFlag),
            argsService.GetStringFromCmdArgs(AutoStartSaveFileNameParam),
            argsService.ContainsInCmdArgs(AutoConnectFlag),
            argsService.GetStringFromCmdArgs(AutoConnectIpParam),
            argsService.GetIntFromCmdArgs(AutoConnectPortParam),
            argsService.GetStringFromCmdArgs(NickParam),
            argsService.GetStringFromCmdArgs(UidParam)
        );
    }
}
