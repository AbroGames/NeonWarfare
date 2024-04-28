using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class PlayerMovementService
{
    
    [EventListener(ListenerSide.Client)]
    public void OnPlayerPhysicsProcessEvent(PlayerPhysicsProcessEvent playerPhysicsProcessEvent) {
        var (player, delta) = playerPhysicsProcessEvent;
        
        var movementInput = GetInput();
        player.MoveAndCollide(movementInput * player.MovementSpeed * delta);

        long nid = Root.Instance.NetworkEntityManager.GetNid(player);
        Network.SendPacketToServer(new ClientMovementPlayerPacket(nid, player.Position.X, player.Position.Y, player.Rotation,
            movementInput.X, movementInput.Y, player.MovementSpeed));
    }

    [EventListener(ListenerSide.Server)]
    public void OnClientMovementPlayerPacket(ClientMovementPlayerPacket clientMovementPlayerPacket)
    {
        Player player = Root.Instance.NetworkEntityManager.GetNode<Player>(clientMovementPlayerPacket.Nid);
        Vector2 newPosition = Vec(clientMovementPlayerPacket.X, clientMovementPlayerPacket.Y);
        
        //TODO проверка расхождение и отправка только если рассхождение большое
        /*if ((player.Position - newPosition).Length() > player.CurrentMovementSpeed / 2)
        {
            Network.SendPacketToPeer(clientMovementPlayerPacket.Sender.Id,
                new ServerPositionEntityPacket(clientMovementPlayerPacket.Nid, player.Position.X, player.Position.Y, player.Rotation));
            player.CurrentMovementVector = Vec(clientMovementPlayerPacket.MovementX, clientMovementPlayerPacket.MovementY);
            player.CurrentMovementSpeed = clientMovementPlayerPacket.MovementSpeed;
            return;
        }*/

        player.Position = newPosition;
        player.Rotation = clientMovementPlayerPacket.Dir;
    }

    [EventListener(ListenerSide.Server)]
    public void OnPlayerPhysicsProcessEvent2(PlayerPhysicsProcessEvent playerPhysicsProcessEvent) //TODO вынести в другое место
    {
        var (player, delta) = playerPhysicsProcessEvent;

        //TODO временно отключаем предсказание движения var movementInput = player.CurrentMovementVector;
        //TODO player.MoveAndCollide(movementInput * player.CurrentMovementSpeed * delta);
        long nid = Root.Instance.NetworkEntityManager.GetNid(player);
        
        foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
        {
            if (playerServerInfo.Player == playerPhysicsProcessEvent.Player) continue; //Отправляем коры игркоа всем кроме самого игрока
            
            Network.SendPacketToPeer(playerServerInfo.Id, new ServerPositionEntityPacket(nid, player.Position.X, player.Position.Y, player.Rotation));
        }
    }
    
    private Vector2 GetInput()
    {
        return Input.GetVector(Keys.Left, Keys.Right, Keys.Up, Keys.Down);
    }
    
}