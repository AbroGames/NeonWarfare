using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

public static class InitServerService
{
    
    public static void InitServer()
    {
        NeonWarfare.Root.Instance.GetWindow().Set("position", new Vector2I(
        DisplayServer.ScreenGetSize().X - (int)NeonWarfare.Root.Instance.GetViewport().GetVisibleRect().Size.X,
        DisplayServer.ScreenGetSize().Y - (int)NeonWarfare.Root.Instance.GetViewport().GetVisibleRect().Size.Y - 40));
        int port = GetPortFromCmdArgs();
        string admin = GetAdminFromCmdArgs();
        int? parentPid = GetParentPidFromCmdArgs();
        ServerParams serverParams = new ServerParams(port, admin, parentPid);
        
        Error error = NetworkOld.CreateDedicatedServer(port);
        if (error == Error.Ok)
        {
            Log.Info($"Dedicated server successfully created.");
            Server server = new Server(serverParams);
            NeonWarfare.ServerRoot.Instance.AddServer(server);
        }
        else
        {
            Log.Error($"Dedicated server created with result: {error}");
        }
    }
    
    //TODO Эту и функции ниже в отдельный CmdArgsService (туда же связанные с CmdArgs вещи из Root)
    public static int GetPortFromCmdArgs()
    {
        int port = DefaultNetworkSettings.Port;
        try
        {
            int portPos = OS.GetCmdlineArgs().ToList().IndexOf(ServerParams.PortParam);
            if (portPos == -1)
            {
                Log.Info($"Port not setup. Use default port: {port}");
                return port;
            }

            port = OS.GetCmdlineArgs()[portPos + 1].ToInt();
        }
        catch
        {
            Log.Warning($"Error while port setup. Use default port: {port}");
            return port;
        }
        
        Log.Info($"Port: {port}");
        return port;
    }
    
    public static string GetAdminFromCmdArgs()
    {
        string admin = null;
        try
        {
            int adminPos = OS.GetCmdlineArgs().ToList().IndexOf(ServerParams.AdminParam);
            if (adminPos == -1)
            {
                Log.Info($"Admin not setup.");
                return null;
            }

            admin = OS.GetCmdlineArgs()[adminPos + 1];
        }
        catch
        {
            Log.Warning("Error while admin setup.");
            return admin;
        }
        
        Log.Info($"Admin: {admin}");
        return admin;
    }
    
    public static int? GetParentPidFromCmdArgs()
    {
        int? parentPid = null;
        try
        {
            int parentPidPos = OS.GetCmdlineArgs().ToList().IndexOf(ServerParams.ParentPidParam);
            if (parentPidPos == -1)
            {
                Log.Info("Parent PID not setup.");
                return null;
            }

            parentPid = OS.GetCmdlineArgs()[parentPidPos + 1].ToInt();
        }
        catch
        {
            Log.Warning($"Error while parent PID setup.");
            return parentPid;
        }
        
        Log.Info($"Parent PID: {parentPid}");
        return parentPid;
    }
}