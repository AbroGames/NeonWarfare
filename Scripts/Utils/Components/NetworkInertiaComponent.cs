using System.Diagnostics;
using Godot;
using KludgeBox.Events;
using NeonWarfare;
using NeonWarfare.Utils.Cooldown;

public partial class NetworkInertiaComponent : Node
{
    public double InertiaCooldown { get; set; } = 0.1;

    private float _movementSpeed;
    private float _movementDir;
    private ManualCooldown _inertiaCooldown;
    private Stopwatch _timeFromLastMovement = new();

    public override void _Ready()
    {
        _inertiaCooldown = new(InertiaCooldown, false, false, () => { _movementSpeed = 0; });
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

    public void OnInertiaEntityPacket(SC_InertiaEntityPacket inertiaEntityPacket)
    {
        GetParent<Node2D>().Position = Vec(inertiaEntityPacket.X, inertiaEntityPacket.Y);
        GetParent<Node2D>().Rotation = inertiaEntityPacket.Dir;
        
        _movementSpeed = inertiaEntityPacket.MovementSpeed;
        _movementDir = inertiaEntityPacket.MovementDir;

        if (inertiaEntityPacket.MovementSpeed != 0)
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
    
    [EventListener(ListenerSide.Client)]
    public static void OnInertiaEntityPacketListener(SC_InertiaEntityPacket inertiaEntityPacket) //TODO подумать над таким неймингом. Записать в readme.
    {
        NetworkInertiaComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetChild<NetworkInertiaComponent>(inertiaEntityPacket.Nid);
        entityComponent.OnInertiaEntityPacket(inertiaEntityPacket);
    }
}