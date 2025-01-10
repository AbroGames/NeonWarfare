using Godot;
using KludgeBox.Networking;
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
        var movementInSecond = movementInput * (float) Player.MovementSpeed;
        Player.MoveAndCollide(movementInSecond * (float) delta);

        if (!CmdArgsService.ContainsInCmdArgs(ServerParams.ServerFlag)) //If is client
        {
            long nid = ClientRoot.Instance.Game.World.OldNetworkEntityManager.GetNid(Player);
            Network.SendToServer(new ClientMovementPlayerPacket(nid, Player.Position.X, Player.Position.Y,
                Player.Rotation,
                movementInSecond.Angle(), movementInSecond.Length()));
        }
    }

    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
}