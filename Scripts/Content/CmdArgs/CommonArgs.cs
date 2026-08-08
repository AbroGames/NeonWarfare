using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct CommonArgs(bool GodotLogPush)
{
    public static readonly string GodotLogPushParam = "--godot-log-push";
    
    public static CommonArgs GetFromCmd(CmdArgsService argsService)
    {
        return new CommonArgs(
            argsService.ContainsInCmdArgs(GodotLogPushParam)
        );
    }
}