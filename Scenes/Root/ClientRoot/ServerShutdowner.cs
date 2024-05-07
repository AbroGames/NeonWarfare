using System.Linq;
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class ServerShutdowner : Node
{
    
    public int? ServerPid { get; set; }
    
    public override void _Notification(int id)
    {
        long[] serverShutdownNotificationTypes =
        [
            NotificationWMCloseRequest, NotificationCrash, NotificationDisabled, NotificationPredelete,
            NotificationExitTree
        ];

        if (ServerPid.HasValue && serverShutdownNotificationTypes.Contains(id) && OS.IsProcessRunning(ServerPid.Value))
        {
            Log.Info($"Kill server process. Pid: {ServerPid.Value}");
            OS.Kill(ServerPid.Value);
        }
    }
}