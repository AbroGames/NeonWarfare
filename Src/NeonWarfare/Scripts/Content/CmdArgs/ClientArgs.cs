using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct ClientArgs(
    CommonArgs CommonArgs,
    bool AutoStart,
    SaveFileName? AutoStartSaveFileName,
    bool AutoConnect,
    string AutoConnectIp,
    int? AutoConnectPort,
    bool AutoHost,
    bool AutoHostDedicated,
    SaveFileName? AutoHostSaveFileName,
    int? AutoHostPort,
    bool AutoHostDedicatedHeadless,
    string Nick,
    PlayerUid? Uid)
{
    public static readonly string AutoStartFlag = "--auto-start";
    public static readonly string AutoStartSaveFileNameParam = "--auto-start-savefile";
    public static readonly string AutoConnectFlag = "--auto-connect";
    public static readonly string AutoConnectIpParam = "--auto-connect-ip";
    public static readonly string AutoConnectPortParam = "--auto-connect-port";
    public static readonly string AutoHostFlag = "--auto-host";
    public static readonly string AutoHostDedicatedFlag = "--auto-host-dedicated";
    public static readonly string AutoHostSaveFileNameParam = "--auto-host-savefile";
    public static readonly string AutoHostPortParam = "--auto-host-port";
    public static readonly string AutoHostDedicatedHeadlessFlag = "--auto-host-dedicated-headless";
    public static readonly string NickParam = "--nick";
    public static readonly string UidParam = "--uid";
    
    public static ClientArgs GetFromCmd(CmdArgsService argsService)
    {
        return new ClientArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.ContainsInCmdArgs(AutoStartFlag),
            argsService.GetParsedFromCmdArgs<SaveFileName>(AutoStartSaveFileNameParam, SaveFileName.TryParse),
            argsService.ContainsInCmdArgs(AutoConnectFlag),
            argsService.GetStringFromCmdArgs(AutoConnectIpParam),
            argsService.GetIntFromCmdArgs(AutoConnectPortParam),
            argsService.ContainsInCmdArgs(AutoHostFlag),
            argsService.ContainsInCmdArgs(AutoHostDedicatedFlag),
            argsService.GetParsedFromCmdArgs<SaveFileName>(AutoHostSaveFileNameParam, SaveFileName.TryParse),
            argsService.GetIntFromCmdArgs(AutoHostPortParam),
            argsService.ContainsInCmdArgs(AutoHostDedicatedHeadlessFlag),
            argsService.GetStringFromCmdArgs(NickParam),
            argsService.GetParsedFromCmdArgs<PlayerUid>(UidParam, PlayerUid.TryParse)
        );
    }
}
