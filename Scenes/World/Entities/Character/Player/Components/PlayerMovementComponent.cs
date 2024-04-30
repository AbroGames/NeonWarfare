using Godot;
using KludgeBox.Net;

namespace NeoVector;

public partial class PlayerMovementComponent : Node
{
    public Player Player { get; private set; } 
    
    public override void _Ready()
    {
        Player = GetParent<Player>();
    }

    public override void _PhysicsProcess(double delta)
    {
        var movementInput = GetInput();
        Player.MoveAndCollide(movementInput * Player.MovementSpeed * delta);

        long nid = Root.Instance.NetworkEntityManager.GetNid(Player);
        NetworkOld.SendPacketToServer(new ClientMovementPlayerPacket(nid, Player.Position.X, Player.Position.Y, Player.Rotation,
            movementInput.X, movementInput.Y, Player.MovementSpeed));
    }

    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}