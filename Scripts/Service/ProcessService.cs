using Godot;
using NeonWarfare.Scripts.Content.CmdArgs;

namespace NeonWarfare.Scripts.Service;

public class ProcessService
{
    
    public int StartNewApplication(string[] arguments)
    {
        return OS.CreateInstance(arguments);
    }
    
    public int StartNewDedicatedServerApplication(string saveFileName, int port, string adminUid, bool showWindow)
    {
        CommonArgs commonArgs = new CommonArgs(
            false); // Dedicated server never uses Godot console
        DedicatedServerArgs dedicatedServerArgs = new DedicatedServerArgs(
            commonArgs,
            !showWindow, 
            port, 
            saveFileName, 
            adminUid, 
            OS.GetProcessId(), 
            false,
            false);

        return StartNewApplication(dedicatedServerArgs.GetArrayToStartDedicatedServer());
    }
}
