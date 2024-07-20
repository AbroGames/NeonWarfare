using System.Collections.Generic;
using Godot;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare.Net;

public static class NetworkService
{

    public const string DefaultHost = "127.0.0.1";
    public const int DefaultPort = 25566;
    
    public static void CreateServer(int port, string adminNickname, bool showConsole)
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
    
    public static void ConnectToServer(string host, int port)
    {
        Netplay.SetClient(host, port);
    }
}