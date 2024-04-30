using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class PlayerMovementService
{

    [EventListener(ListenerSide.Server)]
    public void OnClientMovementPlayerPacket(ClientMovementPlayerPacket clientMovementPlayerPacket)
    {
        Player player = Root.Instance.NetworkEntityManager.GetNode<Player>(clientMovementPlayerPacket.Nid);
        Vector2 newPosition = Vec(clientMovementPlayerPacket.X, clientMovementPlayerPacket.Y);
        long nid = Root.Instance.NetworkEntityManager.GetNid(player);
        
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
        
        foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
        {
            if (playerServerInfo.Player == player) continue; //Отправляем коры игркоа всем кроме самого игрока
            NetworkOld.SendPacketToPeer(playerServerInfo.Id, new ServerPositionEntityPacket(nid, player.Position.X, player.Position.Y, player.Rotation));
        }
    }
}