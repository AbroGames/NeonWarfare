using System.Collections.Generic;
using KludgeBox.Core;

namespace NeonWarfare.Scripts.Content.CmdArgs;

public readonly record struct DedicatedServerArgs(
    CommonArgs CommonArgs,
    int? Port, 
    SaveFileName? SaveFileName, 
    PlayerUid? Admin, 
    int? ParentPid,
    string SettingsFileName,
    string WorldOptionsFileName)
{
    public static readonly string DedicatedServerFlag = "--server";
    
    public static readonly string PortParam = "--port"; 
    public static readonly string SaveFileNameParam = "--savefile";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
    public static readonly string SettingsFileNameParam = "--settings-file";
    public static readonly string WorldOptionsFileNameParam = "--world-options-file";
    
    public static DedicatedServerArgs GetFromCmd(CmdArgsService argsService)
    {
        return new DedicatedServerArgs(
            CommonArgs.GetFromCmd(argsService),
            argsService.GetIntFromCmdArgs(PortParam),
            argsService.GetParsedFromCmdArgs<SaveFileName>(SaveFileNameParam, Scripts.SaveFileName.TryParse),
            argsService.GetParsedFromCmdArgs<PlayerUid>(AdminParam, PlayerUid.TryParse),
            argsService.GetIntFromCmdArgs(ParentPidParam),
            argsService.GetStringFromCmdArgs(SettingsFileNameParam),
            argsService.GetStringFromCmdArgs(WorldOptionsFileNameParam)
        );
    }

    public string[] GetArrayToStartDedicatedServer()
    {
        List<string> listParams = [];
        
        listParams.Add(DedicatedServerFlag);
        
        if (Port is { } port) listParams.AddRange([PortParam, port.ToString()]);
        if (SaveFileName is { } saveFileName) listParams.AddRange([SaveFileNameParam, saveFileName.Value]);
        if (Admin is { } admin) listParams.AddRange([AdminParam, admin.Value]);
        if (ParentPid.HasValue) listParams.AddRange([ParentPidParam, ParentPid.ToString()]);
        if (SettingsFileName != null) listParams.AddRange([SettingsFileNameParam, SettingsFileName]);
        if (WorldOptionsFileName != null) listParams.AddRange([WorldOptionsFileNameParam, WorldOptionsFileName]);
        if (CommonArgs.GodotLogPush) listParams.Add(CommonArgs.GodotLogPushParam);

        return listParams.ToArray();
    }
}
