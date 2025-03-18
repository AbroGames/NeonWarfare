using System;
using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Server;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemyMovementComponent : Node
{
    private const int NetworkMessagePerSecond = 15;

    private long _orderId = 0;
    private ServerEnemy _parent;
    private ServerEnemyTargetComponent _parentTargetComponent;
    private ManualCooldown _sendPositionCooldown = new(1.0/NetworkMessagePerSecond);
    
    /// <summary>
    /// Если отсюда вернется что-то кроме null, это что-то будет использоваться в качестве направления движения
    /// TODO: продумать вариант, когда расстояние до конечной точки оказывается меньше скорости.
    /// </summary>
    public Func<Vector2?> CustomMovementDirectionProvider;
    
    public override void _Ready()
    {
        _parent = GetParent<ServerEnemy>();
        _parentTargetComponent = _parent.GetChild<ServerEnemyTargetComponent>();
        _sendPositionCooldown.ActionWhenReady += () => { SendPositionToServer(); _sendPositionCooldown.Restart(); };
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _sendPositionCooldown.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_parentTargetComponent.Target != null)
        {
            _parent.Velocity = GetMovementInSecondFromAngle();
            _parent.MoveAndSlide();
        }
        else
        {
            _parent.Velocity = Vector2.Zero;
        }
    }

    private void SendPositionToServer()
    {
        var movementInSecond = _parent.Velocity;
        long nid = _parent.GetChild<ServerNetworkEntityComponent>().Nid;
        Network.SendToAll(new NetworkInertiaComponent.SC_InertiaEntityPacket(nid, _orderId++,
            _parent.Position, _parent.Rotation,
            movementInSecond.Angle(), movementInSecond.Length()));
    }

    private Vector2 GetMovementDirection()
    {
        return CustomMovementDirectionProvider?.Invoke() // Сначала пытаемся получить кастомное направление
               ?? Vector2.FromAngle(_parent.GetRotation() - Mathf.DegToRad(90)); // и используем стандартную логику, если кастомная ничего не дала
    }
    private Vector2 GetMovementInSecondFromAngle()
    {
        return GetMovementDirection() * (float) _parent.MovementSpeed;
    }
}
