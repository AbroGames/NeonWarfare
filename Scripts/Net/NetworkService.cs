using Godot;
using KludgeBox;
using KludgeBox.Networking;

namespace NeonWarfare.Net;

public static class NetworkService
{
    
    public static int StartNewDedicatedServerApplication(int port, string adminNickname, bool showConsole)
    {
        ServerParams serverParams = new ServerParams(!showConsole, false, port, adminNickname, OS.GetProcessId());
        
        int serverPid = OS.CreateInstance(serverParams.GetArrayToStartServer());
        return serverPid;
    }
}