using System.Diagnostics;
using Godot;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scripts.Utils.Components;

public partial class NetworkInertiaComponent : Node
{
    public Node2D Parent;
    public double InertiaCooldown { get; set; } = 0.08;

    private float _movementSpeed;
    private float _movementDir;
    private ManualCooldown _inertiaCooldown;
    private Stopwatch _timeFromLastMovement = new();

    public override void _Ready()
    {
        Parent = GetParent<Node2D>();
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
        
        Parent.Position += Vector2.FromAngle(_movementDir) * _movementSpeed * realDelta;
    }

    public void OnInertiaEntityPacket(float x, float y, float dir, float movementDir, float movementSpeed)
    {
        Parent.Position = Vec(x, y);
        Parent.Rotation = dir;
        
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
    
    [EventListener(ListenerSide.Client)]
    public static void OnInertiaEntityPacketListener(SC_InertiaEntityPacket inertiaEntityPacket) //TODO подумать над таким неймингом. Записать в readme.
    {
        //Проверка нужна, т.к. пакет InertiaEntityPacket.Mode = Unreliable и из-за задержки может прийти после пакета смены мира.
        if (!ClientRoot.Instance.Game.World.NetworkEntityManager.HasEntityComponent(inertiaEntityPacket.Nid)) return;
        
        NetworkInertiaComponent entityComponent = ClientRoot.Instance.Game.World.NetworkEntityManager.GetChild<NetworkInertiaComponent>(inertiaEntityPacket.Nid);
        entityComponent.OnInertiaEntityPacket(inertiaEntityPacket.X, inertiaEntityPacket.Y, inertiaEntityPacket.Dir, 
            inertiaEntityPacket.MovementDir, inertiaEntityPacket.MovementSpeed);
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnInertiaEntityPacketListener(CS_InertiaEntityPacket inertiaEntityPacket)
    {
        //Проверка нужна, т.к. пакет InertiaEntityPacket.Mode = Unreliable и из-за задержки может прийти после пакета смены мира.
        if (!ServerRoot.Instance.Game.World.NetworkEntityManager.HasEntityComponent(inertiaEntityPacket.Nid)) return;
        
        NetworkInertiaComponent entityComponent = ServerRoot.Instance.Game.World.NetworkEntityManager.GetChild<NetworkInertiaComponent>(inertiaEntityPacket.Nid);
        entityComponent.OnInertiaEntityPacket(inertiaEntityPacket.X, inertiaEntityPacket.Y, inertiaEntityPacket.Dir, 
            inertiaEntityPacket.MovementDir, inertiaEntityPacket.MovementSpeed);
        Network.SendToAllExclude(inertiaEntityPacket.SenderId, new SC_InertiaEntityPacket(inertiaEntityPacket));
    }
}
