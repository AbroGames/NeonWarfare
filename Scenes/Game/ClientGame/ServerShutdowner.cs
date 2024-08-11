using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerShutdowner : Node
{
    private static long[] _serverShutdownNotificationTypes =
    [
        NotificationWMCloseRequest, 
        NotificationCrash, 
        NotificationDisabled, 
        NotificationPredelete,
        NotificationExitTree
    ];
    
    public int? ServerPid { get; set; }
    
    public override void _Notification(int id)
    {
        if (ServerPid.HasValue && _serverShutdownNotificationTypes.Contains(id) && OS.IsProcessRunning(ServerPid.Value))
        {
            Shutdown();
        }
    }

    public void Shutdown()
    {
        Log.Info($"Kill server process. Pid: {ServerPid.Value}");
        OS.Kill(ServerPid.Value);
    }
}