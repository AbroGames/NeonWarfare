using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class PlayerMovementService
{
    
    [EventListener]
    public void OnPlayerPhysicsProcessEvent(PlayerPhysicsProcessEvent playerPhysicsProcessEvent) {
        var (player, delta) = playerPhysicsProcessEvent;
        
        var movementInput = GetInput();
        player.MoveAndCollide(movementInput * player.MovementSpeed * delta);

        long nid = Root.Instance.NetworkEntityManager.GetNid(player);
        Network.SendPacketToServer(new ClientPositionPlayerPacket(nid, player.Position.X, player.Position.Y, player.Rotation));
    }

    [EventListener]
    public void OnClientPositionPlayerPacket(ClientPositionPlayerPacket clientPositionPlayerPacket)
    {
        Player player = Root.Instance.NetworkEntityManager.GetNode<Player>(clientPositionPlayerPacket.Nid);
        player.Position = Vec(clientPositionPlayerPacket.X, clientPositionPlayerPacket.Y);
        player.Rotation = clientPositionPlayerPacket.Dir;
    }
    
    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
}