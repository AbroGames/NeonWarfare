using System.Collections.Generic;
using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct DedicatedServerArgs(
    CommonArgs CommonArgs,
    int? Port, 
    string SaveFileName, 
    string Admin, 
    int? ParentPid)
{
    public static readonly string DedicatedServerFlag = "--server";
    
    public static readonly string PortParam = "--port";
    public static readonly string SaveFileNameParam = "--savefile";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
    
    public static DedicatedServerArgs GetFromCmd(CmdArgsService argsService)
    {
        return new DedicatedServerArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.GetIntFromCmdArgs(PortParam),
            argsService.GetStringFromCmdArgs(SaveFileNameParam),
            argsService.GetStringFromCmdArgs(AdminParam),
            argsService.GetIntFromCmdArgs(ParentPidParam)
        );
    }

    public string[] GetArrayToStartDedicatedServer()
    {
        List<string> listParams = [];
        
        listParams.Add(DedicatedServerFlag);
        listParams.AddRange([PortParam, Port.ToString()]);
        
        if (SaveFileName != null) listParams.AddRange([SaveFileNameParam, SaveFileName]);
        if (Admin != null) listParams.AddRange([AdminParam, Admin]);
        if (ParentPid.HasValue) listParams.AddRange([ParentPidParam, ParentPid.ToString()]);
        if (CommonArgs.GodotLogPush) listParams.Add(CommonArgs.GodotLogPushParam);

        return listParams.ToArray();
    }
}
