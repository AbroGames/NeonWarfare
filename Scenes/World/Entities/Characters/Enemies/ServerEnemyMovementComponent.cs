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
    private const int NetworkMessagePerSecond = 25;

    private long _orderId = 0;
    private ServerEnemy _parent;
    private ManualCooldown _sendPositionCooldown = new(1.0/NetworkMessagePerSecond);
    
    public override void _Ready()
    {
        _parent = GetParent<ServerEnemy>();
        _sendPositionCooldown.ActionWhenReady += () => { SendPositionToServer(); _sendPositionCooldown.Restart(); };
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _sendPositionCooldown.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _parent.Velocity = GetMovementInSecondFromAngle();
        _parent.MoveAndSlide();
    }

    private void SendPositionToServer()
    {
        var movementInSecond = GetMovementInSecondFromAngle();
        long nid = _parent.GetChild<ServerNetworkEntityComponent>().Nid;
        Network.SendToAll(new NetworkInertiaComponent.SC_InertiaEntityPacket(nid, _orderId++,
            _parent.Position, _parent.Rotation,
            movementInSecond.Angle(), movementInSecond.Length()));
    }

    private Vector2 GetMovementInSecondFromAngle()
    {
        return Vector2.FromAngle(_parent.GetRotation() - Mathf.DegToRad(90)) * (float) _parent.MovementSpeed;
    }
}
