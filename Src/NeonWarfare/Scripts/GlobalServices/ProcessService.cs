using Godot;
using NeonWarfare.Scripts.Content.CmdArgs;

namespace NeonWarfare.Scripts.GlobalServices;

public class ProcessService
{
    
    // Godot's own flag, not a game argument: the engine consumes it before the game sees the command line
    private const string GodotHeadlessFlag = "--headless";
    
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
            port, 
            saveFileName, 
            adminUid, 
            OS.GetProcessId());

        string[] arguments = dedicatedServerArgs.GetArrayToStartDedicatedServer();
        return StartNewApplication(showWindow ? arguments : [..arguments, GodotHeadlessFlag]);
    }
}
