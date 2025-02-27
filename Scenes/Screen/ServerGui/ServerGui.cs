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

public partial class ServerGui : Control
{

    [Export] [NotNull] public Label Fps {get; set;}
    [Export] [NotNull] public Label Tps {get; set;}
    [Export] [NotNull] public Label Info {get; set;}

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(true);
    }

    public override void _Process(double delta)
    {
        Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
        Tps.Text = $"TPS: {Math.Min(1.0/Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess), Engine.PhysicsTicksPerSecond):N0}";
        
        Info.Text = $"Nodes: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}";
        Info.Text += $"\nWorld 1-level nodes: {ServerRoot.Instance.Game.World.GetChildCount()}";
        Info.Text += $"\nFrame time process: {Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:N1}ms";
        Info.Text += $"\nPhysics time process: {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)*1000:N1}ms ({Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * Engine.PhysicsTicksPerSecond * 100:N0} %)";

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
