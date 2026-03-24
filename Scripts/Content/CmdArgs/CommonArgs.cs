namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct CommonArgs(bool GodotLogPush)
{
    public static readonly string GodotLogPushParam = "--godot-log-push";
    
    public static CommonArgs GetFromCmd(KludgeBox.Core.CmdArgsService argsService)
    {
        return new CommonArgs(
            argsService.ContainsInCmdArgs(GodotLogPushParam)
        );
    }
}