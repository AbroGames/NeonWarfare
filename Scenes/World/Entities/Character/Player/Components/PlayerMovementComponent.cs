using Godot;
using KludgeBox.Networking;
using NeonWarfare.Net;
using NeonWarfare.Utils;

namespace NeonWarfare.Components;

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

        if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is client
        {
            long nid = ClientRoot.Instance.Game.World.NetworkEntityManager.GetNid(Player);
            Network.SendToServer(new ClientMovementPlayerPacket(nid, Player.Position.X, Player.Position.Y,
                Player.Rotation,
                movementInput.X, movementInput.Y, Player.MovementSpeed));
        }
    }

    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}