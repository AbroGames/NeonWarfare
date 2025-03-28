using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

public partial class ServerGui : Control
{

    [Export] [NotNull] public Label Fps {get; set;}
    [Export] [NotNull] public Label Tps {get; set;}
    [Export] [NotNull] public Label Info {get; set;}


    public long SendPacketsLast;
    public long ReceivedPacketsLast;
    public long SendBytesLast;
    public long ReceivedBytesLast;
    
    public AutoCooldown Cooldown;
    public long SendPacketsInfo;
    public long ReceivedPacketsInfo;
    public long SendBytesInfo;
    public long ReceivedBytesInfo;

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(true);
        Cooldown = new AutoCooldown(1, true, () =>
        {
            SendPacketsInfo = Network.SendPackets - SendPacketsLast;
            ReceivedPacketsInfo = Network.ReceivedPackets - ReceivedPacketsLast;
            SendBytesInfo = Network.SendBytes - SendBytesLast;
            ReceivedBytesInfo = Network.ReceivedBytes - ReceivedBytesLast;
            
            SendPacketsLast = Network.SendPackets;
            ReceivedPacketsLast= Network.ReceivedPackets;
            SendBytesLast = Network.SendBytes;
            ReceivedBytesLast = Network.ReceivedBytes;
        });
    }

    public override void _Process(double delta)
    {
        Cooldown.Update(delta);
        
        Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
        Tps.Text = $"TPS: {Math.Min(1.0/Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess), Engine.PhysicsTicksPerSecond):N0}";
        
        Info.Text = $"Nodes: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}";
        Info.Text += $"\nWorld 1-level nodes: {ServerRoot.Instance.Game.World.GetChildCount()}";
        Info.Text += $"\nFrame time process: {Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:N1}ms";
        Info.Text += $"\nPhysics time process: {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)*1000:N1}ms ({Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * Engine.PhysicsTicksPerSecond * 100:N0} %)";

        Info.Text += $"\nPackets (S/R): {SendPacketsInfo}/{ReceivedPacketsInfo}";
        Info.Text += $"\nKBytes (S/R): {SendBytesInfo/1000}/{ReceivedBytesInfo/1000}";
        
        if (ServerRoot.Instance.Game.World is ServerBattleWorld)
        {
            ServerBattleWorld battleWorld = ServerRoot.Instance.Game.World as ServerBattleWorld;
            Info.Text += "\n\nBattle world.";
            Info.Text += $"\nWave: {battleWorld.EnemyWave.WaveNumber}.";
            Info.Text += $"\nTimer: {battleWorld.EnemyWave.NextWaveCooldown.TimeLeft:N1}.";
            Info.Text += $"\nEnemies: {battleWorld.Enemies.Count:N0}.";
        }
        if (ServerRoot.Instance.Game.World is ServerSafeWorld)
        {
            ServerSafeWorld safeWorld = ServerRoot.Instance.Game.World as ServerSafeWorld;
            Info.Text += "\n\nSafe world.";
        }
    }
}
