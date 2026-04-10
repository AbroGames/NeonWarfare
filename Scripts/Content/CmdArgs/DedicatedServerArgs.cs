using System.Collections.Generic;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct DedicatedServerArgs(
    CommonArgs CommonArgs,
    bool IsHeadless, 
    int? Port, 
    string SaveFileName, 
    string Admin, 
    int? ParentPid, 
    bool IsNoHud,
    bool IsWorldRender)
{
    public static readonly string DedicatedServerFlag = "--server";
    
    public static readonly string HeadlessFlag = "--headless";
    public static readonly string PortParam = "--port";
    public static readonly string SaveFileNameParam = "--savefile";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
    public static readonly string NoHudParam = "--no-hud";
    public static readonly string WorldRenderParam = "--world-render";
    
    public static DedicatedServerArgs GetFromCmd(KludgeBox.Core.CmdArgsService argsService)
    {
        return new DedicatedServerArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.ContainsInCmdArgs(HeadlessFlag),
            argsService.GetIntFromCmdArgs(PortParam),
            argsService.GetStringFromCmdArgs(SaveFileNameParam),
            argsService.GetStringFromCmdArgs(AdminParam),
            argsService.GetIntFromCmdArgs(ParentPidParam),
            argsService.ContainsInCmdArgs(NoHudParam),
            argsService.ContainsInCmdArgs(WorldRenderParam)
        );
    }

    public string[] GetArrayToStartDedicatedServer()
    {
        List<string> listParams = [];
        
        listParams.Add(DedicatedServerFlag);
        listParams.AddRange([PortParam, Port.ToString()]);
        
        if (IsHeadless) listParams.Add(HeadlessFlag);
        if (SaveFileName != null) listParams.AddRange([SaveFileNameParam, SaveFileName]);
        if (Admin != null) listParams.AddRange([AdminParam, Admin]);
        if (ParentPid.HasValue) listParams.AddRange([ParentPidParam, ParentPid.ToString()]);
        if (IsNoHud) listParams.Add(NoHudParam);
        if (IsWorldRender) listParams.Add(WorldRenderParam);
        if (CommonArgs.GodotLogPush) listParams.Add(CommonArgs.GodotLogPushParam);

        return listParams.ToArray();
    }
}
