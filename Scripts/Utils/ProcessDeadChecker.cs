using System;
using System.Diagnostics;
using System.Linq;
using Godot;
using KludgeBox;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare;

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
        if (ProcessPid.HasValue && !Process.GetProcesses().Any(x => x.Id == ProcessPid.Value))
        {
            Log.Info(LogMessageGenerator(ProcessPid.Value));
            ActionWhenDead?.Invoke();
        }
    }
}