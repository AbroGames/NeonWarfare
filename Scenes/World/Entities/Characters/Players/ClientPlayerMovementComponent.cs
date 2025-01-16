using Godot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;
using NeonWarfare.Scripts.Utils.NetworkEntityManager.Client;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayerMovementComponent : Node
{
    private const int NetworkMessagePerSecond = 30;

    private ClientPlayer _parent;
    private ManualCooldown _sendPositionCooldown = new(1.0/NetworkMessagePerSecond);
    
    public override void _Ready()
    {
        _parent = GetParent<ClientPlayer>();
        _sendPositionCooldown.ActionWhenReady += () => { SendPositionToServer(); _sendPositionCooldown.Restart(); };
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        _sendPositionCooldown.Update(delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        _parent.MoveAndCollide(GetMovementInSecondFromInput() * (float) delta);
    }

    private void SendPositionToServer()
    {
        var movementInSecond = GetMovementInSecondFromInput();
        long nid = _parent.GetChild<ClientNetworkEntityComponent>().Nid;
        Network.SendToServer(new NetworkInertiaComponent.CS_InertiaEntityPacket(nid, 
            _parent.Position.X, _parent.Position.Y, _parent.Rotation,
            movementInSecond.Angle(), movementInSecond.Length()));
    }

    private Vector2 GetMovementInSecondFromInput()
    {
        var movementInput = GetInput();
        return movementInput * (float) _parent.MovementSpeed;
    }

    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}
