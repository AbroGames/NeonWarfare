using Godot;
using NeonWarfare.Scripts.Service.CmdArgs;

namespace NeonWarfare.Scripts.Service;

public class ProcessService
{
    
    public int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }
    
    public int StartNewDedicatedServerApplication(int port, string saveFileName, string adminNickname, bool showWindow)
    {
        DedicatedServerArgs dedicatedServerArgs = new DedicatedServerArgs(!showWindow, port, saveFileName, adminNickname, OS.GetProcessId(), false, false);

        return StartNewApplication(dedicatedServerArgs.GetArrayToStartDedicatedServer());
    }
    
    public int StartNewClientApplicationAndAutoConnect(string ip, int port)
    {
        string[] clientParams = [ClientArgs.AutoConnectIpFlag, ip, ClientArgs.AutoConnectPortFlag, port.ToString()];
        return StartNewApplication(clientParams);
    }
}
