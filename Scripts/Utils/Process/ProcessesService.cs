using Godot;
using NeonWarfare.Scripts.Utils.CmdArgs;

namespace NeonWarfare.Scripts.Utils.Process;

public static class ProcessesService
{
    
    public static int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }
    
    public static int StartNewDedicatedServerApplication(int port, string adminNickname, bool showGui)
    {
        ServerParams serverParams = new ServerParams(!showGui, port, adminNickname, OS.GetProcessId());

        return StartNewApplication(serverParams.GetArrayToStartServer());
    }
    
    public static int StartNewClientApplicationAndAutoConnect(string ip, int port)
    {
        string[] clientParams = [ClientParams.AutoConnectIpFlag, ip, ClientParams.AutoConnectPortFlag, port.ToString()];
        return StartNewApplication(clientParams);
    }
}
