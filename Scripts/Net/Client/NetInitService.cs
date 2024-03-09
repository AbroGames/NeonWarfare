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
        Root.Instance.ServerPid = OS.CreateInstance([
            ServerParams.ServerFlag,
            ServerParams.HeadlessFlag,
            ServerParams.PortParam, createServerRequest.Port.ToString(),
            ServerParams.AdminParam, createServerRequest.AdminNickname,
            ServerParams.ParentPidParam, OS.GetProcessId().ToString()
        ]);
    }
    
    [EventListener]
    public void OnConnectToServerRequest(ConnectToServerRequest connectToServerRequest)
    {
        Network.ConnectToRemoteServer(DefaultNetworkSettings.Host, DefaultNetworkSettings.Port);
    }
}