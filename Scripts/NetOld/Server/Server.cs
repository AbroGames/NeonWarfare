using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using KludgeBox.Events.Global;
using KludgeBox.Scheduling;
using NeonWarfare.Net;

namespace NeonWarfare.NetOld.Server;

public partial class Server : Node
{
    public Cooldown IsParentDeadChecker { get; set; } = new(5);
    
    public IDictionary<long, PlayerServerInfo> PlayerServerInfo { get; private set; } = new Dictionary<long, PlayerServerInfo>();
    
    public override void _Ready()
    {
        IsParentDeadChecker.Ready += CheckParentIsDead;
        Log.Info("Server ready!");
    }    
    
    public override void _Process(double delta)
    {
        IsParentDeadChecker.Update(delta);
    }
    
    public void CheckParentIsDead()
    {
        int? parentPid = ServerRoot.Instance.CmdParams.ParentPid;
        
        if (parentPid.HasValue && !Process.GetProcesses().Any(x => x.Id == parentPid.Value))
        {
            Log.Error($"Parent process {parentPid.Value} is dead. Shutdown server.");
            ServerRoot.Instance.Shutdown();
        }
    }
}