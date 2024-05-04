using System.Collections.Generic;
using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class NetInitService
{
    [EventListener]
    public void OnCreateServerRequest(CreateServerRequest createServerRequest)
    {
        List<string> serverParams =
        [
            ServerParams.ServerFlag,
            ServerParams.PortParam, createServerRequest.Port.ToString(),
            ServerParams.AdminParam, createServerRequest.AdminNickname,
            ServerParams.ParentPidParam, OS.GetProcessId().ToString()
        ];

        if (!createServerRequest.ShowConsole)
        {
            serverParams.Add(ServerParams.HeadlessFlag);
        }
        
        //serverParams.Add(ServerParams.RenderFlag); //TODO del or to Config node in Root
        NeonWarfare.ClientRoot.Instance.ServerShutdowner.ServerPid = OS.CreateInstance(serverParams.ToArray());
    }
    
    [EventListener]
    public void OnConnectToServerRequest(ConnectToServerRequest connectToServerRequest)
    {
        NetworkOld.ConnectToRemoteServer(connectToServerRequest.Host, connectToServerRequest.Port);
    }
}