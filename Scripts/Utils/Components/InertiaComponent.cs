using System.Diagnostics;
using Godot;
using KludgeBox.Events;
using NeonWarfare.Utils.Cooldown;

namespace NeonWarfare.Utils.Components;

public partial class InertiaComponent : Node
{
    private const double InertiaCooldown = 0.1;

    private float _movementSpeed;
    private float _movementDir;
    private ManualCooldown _inertiaCooldown = new(InertiaCooldown, false, false);
    private Stopwatch _timeFromLastMovement = new();

    public override void _Ready()
    {
        _inertiaCooldown.ActionWhenReady += () => { _movementSpeed = 0; };
    }
    
    public override void _Process(double delta)
    {
        _inertiaCooldown.Update(delta);

        //Первый вызов _Process после вызова OnParentMovement будет выполнен не через delta секунд.
        //Необходимо корректно посчитать это время таймером.
        //Далее таймер не требуется, до следующего вызова OnParentMovement.
        float realDelta = (float) delta;
        if (_timeFromLastMovement.IsRunning) 
        {
            realDelta = (float) _timeFromLastMovement.Elapsed.TotalSeconds;
            _timeFromLastMovement.Reset();
        }
        
        GetParent<Node2D>().Position += Vector2.FromAngle(_movementDir) * _movementSpeed * realDelta;
    }

    public void OnParentMovement(float movementSpeed, float movementDir)
    {
        _movementSpeed = movementSpeed;
        _movementDir = movementDir;

        if (movementSpeed != 0)
        {
            _inertiaCooldown.Restart();
            _timeFromLastMovement.Restart();
        }
        else
        {
            _inertiaCooldown.Reset();
            _timeFromLastMovement.Reset();
        }
    }
}