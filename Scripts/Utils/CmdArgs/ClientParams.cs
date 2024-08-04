using NeonWarfare.Utils;

namespace NeonWarfare.Net;

public readonly record struct ClientParams(bool AutoTest)
{
    public static readonly string AutoTestFlag = "--auto-test";
    
    public static ClientParams GetFromCmd()
    {
        return new ClientParams(
            CmdArgsService.ContainsInCmdArgs(AutoTestFlag)
        );
    }
}