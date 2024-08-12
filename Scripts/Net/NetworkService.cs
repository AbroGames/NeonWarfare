using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare.Net;

public static class NetworkService
{

    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 25566;
    
    public static void CreateServer(int port)
    {
        Error error = Network.SetServer(port);
        if (error == Error.Ok)
        {
            Log.Info($"Network successfully created.");
        }
        else
        {
            Log.Error($"Create network with result: {error}");
        }
    }
    
    public static void ConnectToServer(string host, int port)
    {
        Error error = Network.SetClient(host, port);
        if (error == Error.Ok)
        {
            Log.Info($"Network successfully created.");
        }
        else
        {
            Log.Error($"Create network with result: {error}");
        }
    }
    
    public static int StartNewDedicatedServerApplication(int port, string adminNickname, bool showConsole)
    {
        ServerParams serverParams = new ServerParams(!showConsole, false, port, adminNickname, OS.GetProcessId());
        
        int serverPid = OS.CreateInstance(serverParams.GetArrayToStartServer());
        return serverPid;
    }
}