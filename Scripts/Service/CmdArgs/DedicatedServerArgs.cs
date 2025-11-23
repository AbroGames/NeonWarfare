using System.Collections.Generic;

namespace NeonWarfare.Scripts.Service.CmdArgs;

public readonly record struct DedicatedServerArgs(
    bool IsHeadless, 
    int? Port, 
    string SaveFileName, 
    string Admin, 
    int? ParentPid, 
    bool IsRender, 
    bool GodotLogPush)
{
    public static readonly string DedicatedServerFlag = "--server";
    
    public static readonly string HeadlessFlag = "--headless";
    public static readonly string PortParam = "--port";
    public static readonly string SaveFileNameParam = "--savefile";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
    public static readonly string RenderParam = "--render";
    public static readonly string GodotLogPushParam = "--godot-log-push";
    
    public static DedicatedServerArgs GetFromCmd(KludgeBox.Core.CmdArgsService argsService)
    {
        return new DedicatedServerArgs(
            argsService.ContainsInCmdArgs(HeadlessFlag),
            argsService.GetIntFromCmdArgs(PortParam),
            argsService.GetStringFromCmdArgs(SaveFileNameParam),
            argsService.GetStringFromCmdArgs(AdminParam),
            argsService.GetIntFromCmdArgs(ParentPidParam),
            argsService.ContainsInCmdArgs(RenderParam),
            argsService.ContainsInCmdArgs(GodotLogPushParam)
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
        if (IsRender) listParams.Add(RenderParam);
        if (GodotLogPush) listParams.Add(GodotLogPushParam);

        return listParams.ToArray();
    }
}
