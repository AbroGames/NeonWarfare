using Godot;
using KludgeBox;
using KludgeBox.Networking;

namespace NeonWarfare;

public static class ProcessesService
{
    
    public static int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }
    
    public static int StartNewDedicatedServerApplication(int port, string adminNickname, bool showConsole)
    {
        ServerParams serverParams = new ServerParams(!showConsole, false, port, adminNickname, OS.GetProcessId());

        return StartNewApplication(serverParams.GetArrayToStartServer());
    }
    
    public static int StartNewClientApplicationAndAutoConnect(string ip, int port)
    {
        string[] clientParams = [ClientParams.AutoConnectIpFlag, ip, ClientParams.AutoConnectPortFlag, port.ToString()];
        return StartNewApplication(clientParams);
    }
}