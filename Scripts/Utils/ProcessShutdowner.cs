using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ProcessShutdowner : Node
{
    private static long[] _processShutdownNotificationTypes =
    [
        NotificationWMCloseRequest, 
        NotificationCrash, 
        NotificationDisabled, 
        NotificationPredelete,
        NotificationExitTree
    ];
    
    public int? ProcessPid { get; set; }
    public string LogMessage { get; set; } = "Kill process.";
    
    public override void _Notification(int id)
    {
        if (ProcessPid.HasValue && _processShutdownNotificationTypes.Contains(id) && OS.IsProcessRunning(ProcessPid.Value))
        {
            Shutdown();
        }
    }

    public void Shutdown()
    {
        Log.Info($"{LogMessage} Pid: {ProcessPid.Value}");
        OS.Kill(ProcessPid.Value);
    }
}