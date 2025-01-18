using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.BattleWorld.ServerBattleWorld;
using NeonWarfare.Scripts.KludgeBox.Core;

public partial class ServerGui : Control
{

    [Export] [NotNull] public Label Fps {get; set;}
    [Export] [NotNull] public Label Tps {get; set;}
    [Export] [NotNull] public Label Info {get; set;}

    private const int MAX_TPS = 60; //TODO брать из ProjectSettings.GetSetting("physics/common/physics_ticks_per_second")
    
    private readonly Stopwatch _physicsStopwatch = new();
    private readonly Queue<double> _deltas = new();

    public override void _Ready()
    {
        NotNullChecker.CheckProperties(true);
    }

    public override void _Process(double delta)
    {
        Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
        Info.Text = $"Nodes: {Performance.GetMonitor(Performance.Monitor.ObjectNodeCount)}";
        Info.Text += $"\nWorld 1-level nodes: {ServerRoot.Instance.Game.World.GetChildCount()}";
        Info.Text += $"\nFrame time process: {Performance.GetMonitor(Performance.Monitor.TimeProcess)*1000:N1}ms";
        Info.Text += $"\nPhysics time process: {Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess)*1000:N1}ms ({Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * MAX_TPS * 100:N0} %)";

        if (ServerRoot.Instance.Game.World is ServerBattleWorld)
        {
            ServerBattleWorld battleWorld = ServerRoot.Instance.Game.World as ServerBattleWorld;
            Info.Text += $"\n\nBattle. Wave: {battleWorld.EnemyWave.WaveNumber}. Timer: {battleWorld.EnemyWave.NextWaveCooldown.TimeLeft:N1}.";
        }
    }
    
    /// <summary>
    /// Мы используем настолько альтернативный подход к расчету ТПС потому, что все значения физической дельты в движке - константы.
    /// Даже если реальный ТПС упадёт до 1, дельта, приходящая в _PhysicsProcess будет 1/60.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        var realDelta = _physicsStopwatch.Elapsed.TotalSeconds;
        
        _deltas.Enqueue(realDelta);
        if (_deltas.Count >= 120)
        {
            var tps = _deltas.Average();
            Tps.Text = $"TPS: {1/tps:N0}";
            _deltas.Dequeue();
        }
        _physicsStopwatch.Restart();
    }
}
