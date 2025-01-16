using System;
using System.Linq;
using Godot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scripts.Utils.Process;

public partial class ProcessDeadChecker : Node
{
    
    public int? ProcessPid { get; set; }
    public Func<int, string> LogMessageGenerator { get; set; } = pid => $"Process {pid} is dead.";
    public Action ActionWhenDead { get; set; }
    
    private AutoCooldown ProcessDeadCheckCooldown = new(5);

    public override void _Ready()
    {
        ProcessDeadCheckCooldown.ActionWhenReady += CheckProcessIsDead;
    }

    public override void _Process(double delta)
    {
        ProcessDeadCheckCooldown.Update(delta);
    }

    public void CheckProcessIsDead()
    {
        if (ProcessPid.HasValue && !System.Diagnostics.Process.GetProcesses().Any(x => x.Id == ProcessPid.Value))
        {
            Log.Info(LogMessageGenerator(ProcessPid.Value));
            ActionWhenDead?.Invoke();
        }
    }
}
