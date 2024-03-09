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
        if (!OS.GetCmdlineArgs().Contains("--server")) return;
        
        int port = EventBus.Require(new GetPortFromCmdArgsQuery());
        string admin = EventBus.Require(new GetAdminFromCmdArgsQuery());
    }
    
    [EventListener]
    public int OnGetPortFromCmdArgsQuery(GetPortFromCmdArgsQuery getPortFromCmdArgsQuery)
    {
        int port = DefaultNetworkSettings.Port;
        try
        {
            int portPos = OS.GetCmdlineArgs().ToList().IndexOf("--port");
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
            int adminPos = OS.GetCmdlineArgs().ToList().IndexOf("--admin");
            if (adminPos == -1)
            {
                Log.Info($"Admin not setup.");
                return null;
            }

            admin = OS.GetCmdlineArgs()[adminPos + 1];
        }
        catch
        {
            Log.Warning($"Error while admin setup.");
            return admin;
        }
        
        Log.Info($"Admin: {admin}");
        return admin;
    }
}