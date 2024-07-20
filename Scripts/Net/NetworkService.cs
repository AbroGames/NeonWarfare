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
    
    public static ServerParams CreateServer()
    {
        ServerParams serverParams = ServerCmdService.GetServerParams();
        Error error = Netplay.SetServer(serverParams.Port);
        if (error == Error.Ok)
        {
            Log.Info($"Dedicated server successfully created.");
        }
        else
        {
            Log.Error($"Dedicated server created with result: {error}");
        }
        
        return serverParams;
    }
    
    public static void ConnectToServer(string host, int port)
    {
        Error error = Netplay.SetClient(host, port);
        if (error == Error.Ok)
        {
            Log.Info($"Connect to server is successfully.");
        }
        else
        {
            Log.Error($"Connect to server with result: {error}");
        }
    }
    
    public static void CreateDedicatedServerApplication(int port, string adminNickname, bool showConsole)
    {
        List<string> serverParams =
        [
            ServerParams.ServerFlag,
            ServerParams.PortParam, port.ToString(),
            ServerParams.AdminParam, adminNickname,
            ServerParams.ParentPidParam, OS.GetProcessId().ToString()
        ];

        if (!showConsole)
        {
            serverParams.Add(ServerParams.HeadlessFlag);
        }
        
        //serverParams.Add(ServerParams.RenderFlag); //TODO del or to Config node in Root
        int serverPid = OS.CreateInstance(serverParams.ToArray());
        ClientRoot.Instance.AddServerShutdowner(serverPid);
    }
}