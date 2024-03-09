using System;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Events.Global;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class InitServerService
{
    [EventListener]
    public void OnInitServerRequest(InitServerRequest initServerRequest)
    {
        if (!OS.GetCmdlineArgs().Contains(ServerParams.ServerFlag)) return;
        
        int port = EventBus.Require(new GetPortFromCmdArgsQuery());
        string admin = EventBus.Require(new GetAdminFromCmdArgsQuery());
        int? parentPid = EventBus.Require(new GetParentPidFromCmdArgsQuery());
        ServerParams serverParams = new ServerParams(port, admin, parentPid);
        
        Error error = Network.CreateDedicatedServer(port);
        if (error == Error.Ok)
        {
            Log.Info($"Dedicated server successfully created.");
            Server server = new Server(serverParams);
            Root.Instance.AddServer(server);
        }
        else
        {
            Log.Error($"Dedicated server created with result: {error}");
        }
    }
    
    [EventListener]
    public int OnGetPortFromCmdArgsQuery(GetPortFromCmdArgsQuery getPortFromCmdArgsQuery)
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
    
    [EventListener]
    public string OnGetAdminFromCmdArgsQuery(GetAdminFromCmdArgsQuery getAdminFromCmdArgsQuery)
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
    
    [EventListener]
    public int? OnGetParentPidFromCmdArgsQuery(GetParentPidFromCmdArgsQuery getParentPidFromCmdArgsQuery)
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