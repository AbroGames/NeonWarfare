using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Networking;
using NeonWarfare.NetOld.Server;

namespace NeonWarfare;

[GamePacket]
public class ClientWantToBattlePacket : BinaryPacket;

public static class SafeHudService
{

    [EventListener(ListenerSide.Server)]
    public static void OnClientWantToBattlePacket(ClientWantToBattlePacket clientWantToBattlePacket)
    {
        Network.SendToAll(new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Battle));
        ServerBattleWorld serverBattleWorld = new ServerBattleWorld();
        ServerRoot.Instance.Game.ChangeMainScene(serverBattleWorld);
        
        ServerRoot.Instance.Game.NetworkEntityManager.Clear();
        
        foreach (PlayerServerInfo playerServerInfo in ServerRoot.Instance.Game.Server.PlayerServerInfo.Values)
        {
            Player player = ServerRoot.Instance.PackedScenes.Common.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));
            
            if (serverBattleWorld.GetChild<Camera>() == null)
            {
                serverBattleWorld.Player = player;
                
                var camera = new Camera(); //TODO del from server!!
                camera.Position = player.Position;
                camera.TargetNode = player;
                camera.Zoom = Vec(0.65);
                camera.SmoothingPower = 1.5;
                serverBattleWorld.AddChild(camera);
                camera.Enabled = true;
            }

            playerServerInfo.Player = player;
            serverBattleWorld.AddChild(player);
            long newPlayerNid = ServerRoot.Instance.Game.NetworkEntityManager.AddEntity(player);
            
            Network.SendToClient(playerServerInfo.Id,  
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (PlayerServerInfo allyServerInfo in ServerRoot.Instance.Game.Server.PlayerServerInfo.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                Network.SendToClient(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
    }
}