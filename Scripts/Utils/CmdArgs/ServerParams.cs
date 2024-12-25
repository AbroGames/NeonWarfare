using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare.Utils;

namespace NeonWarfare;

public readonly record struct ServerParams(bool IsHeadless, bool IsRender, int Port, string Admin, int? ParentPid)
{
    public static readonly string ServerFlag = "--server";
    
    public static readonly string HeadlessFlag = "--headless";
    public static readonly string RenderFlag = "--render";
    public static readonly string PortParam = "--port";
    public static readonly string AdminParam = "--admin";
    public static readonly string ParentPidParam = "--parent-pid";
    
    public static ServerParams GetFromCmd()
    {
        return new ServerParams(
            CmdArgsService.ContainsInCmdArgs(HeadlessFlag),
            CmdArgsService.ContainsInCmdArgs(RenderFlag),
            CmdArgsService.GetIntFromCmdArgs(PortParam, Network.DefaultPort),
            CmdArgsService.GetStringFromCmdArgs(AdminParam),
            CmdArgsService.GetIntFromCmdArgs(ParentPidParam)
        );
    }

    public string[] GetArrayToStartServer()
    {
        List<string> listParams = [ServerFlag];

        if (IsHeadless)
        {
            listParams.Add(HeadlessFlag);
        }
        if (IsRender)
        {
            listParams.Add(RenderFlag);
        }

        listParams.AddRange([
            PortParam, Port.ToString(),
            AdminParam, Admin
        ]);

        if (ParentPid.HasValue)
        {
            listParams.AddRange([ParentPidParam, ParentPid.ToString()]);
        }

        return listParams.ToArray();
    }
}