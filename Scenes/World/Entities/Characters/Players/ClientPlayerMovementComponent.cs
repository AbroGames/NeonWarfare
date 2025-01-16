using Godot;
using KludgeBox.Networking;
using NeonWarfare;
using NeonWarfare.Utils;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayerMovementComponent : Node
{
    public ClientPlayer Parent { get; private set; } 
    
    public override void _Ready()
    {
        Parent = GetParent<ClientPlayer>();
    }

    public override void _PhysicsProcess(double delta)
    {
        var movementInput = GetInput();
        var movementInSecond = movementInput * (float) Parent.MovementSpeed;
        Parent.MoveAndCollide(movementInSecond * (float) delta);

        long nid = Parent.GetChild<ClientNetworkEntityComponent>().Nid;
        Network.SendToServer(new NetworkInertiaComponent.CS_InertiaEntityPacket(nid, 
            Parent.Position.X, Parent.Position.Y, Parent.Rotation,
            movementInSecond.Angle(), movementInSecond.Length()));
    }

    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}
