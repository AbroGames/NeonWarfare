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
    public double InertiaCooldown { get; set; } = 0.1;

    private long _lastOrderId = -1;
    private float _movementSpeed;
    private float _movementRotation;
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
        
        Parent.Position += Vector2.FromAngle(_movementRotation) * _movementSpeed * realDelta;
    }

    public void OnInertiaEntityPacket(long orderId, Vector2 position, float rotation, float movementRotation, float movementSpeed)
    {
        if (orderId <= _lastOrderId) return;
        _lastOrderId = orderId;
        
        Parent.Position = position;
        Parent.Rotation = rotation;
        
        _movementSpeed = movementSpeed;
        _movementRotation = movementRotation;

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
        entityComponent.OnInertiaEntityPacket(inertiaEntityPacket.OrderId, inertiaEntityPacket.Position, inertiaEntityPacket.Rotation, 
            inertiaEntityPacket.MovementRotation, inertiaEntityPacket.MovementSpeed);
    }
    
    [EventListener(ListenerSide.Server)]
    public static void OnInertiaEntityPacketListener(CS_InertiaEntityPacket inertiaEntityPacket)
    {
        //Проверка нужна, т.к. пакет InertiaEntityPacket.Mode = Unreliable и из-за задержки может прийти после пакета смены мира.
        if (!ServerRoot.Instance.Game.World.NetworkEntityManager.HasEntityComponent(inertiaEntityPacket.Nid)) return;
        
        NetworkInertiaComponent entityComponent = ServerRoot.Instance.Game.World.NetworkEntityManager.GetChild<NetworkInertiaComponent>(inertiaEntityPacket.Nid);
        entityComponent.OnInertiaEntityPacket(inertiaEntityPacket.OrderId, inertiaEntityPacket.Position, inertiaEntityPacket.Rotation, 
            inertiaEntityPacket.MovementRotation, inertiaEntityPacket.MovementSpeed);
        Network.SendToAllExclude(inertiaEntityPacket.SenderId, new SC_InertiaEntityPacket(inertiaEntityPacket));
    }
}
