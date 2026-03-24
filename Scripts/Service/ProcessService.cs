using Godot;
using NeonWarfare.Scripts.Content.CmdArgs;

namespace NeonWarfare.Scripts.Service;

public class ProcessService
{
    
    public int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }
    
    public int StartNewDedicatedServerApplication(int port, string saveFileName, string adminNickname, bool showWindow)
    {
        CommonArgs commonArgs = new CommonArgs(
            false); // Dedicated server never uses Godot console
        DedicatedServerArgs dedicatedServerArgs = new DedicatedServerArgs(
            commonArgs,
            !showWindow, 
            port, 
            saveFileName, 
            adminNickname, 
            OS.GetProcessId(), 
            false); // Can be changed to true only for debug reasons

        return StartNewApplication(dedicatedServerArgs.GetArrayToStartDedicatedServer());
    }
    
    public int StartNewClientApplicationAndAutoConnect(string ip, int port)
    {
        string[] clientParams = [ClientArgs.AutoConnectIpFlag, ip, ClientArgs.AutoConnectPortFlag, port.ToString()];
        return StartNewApplication(clientParams);
    }
}
