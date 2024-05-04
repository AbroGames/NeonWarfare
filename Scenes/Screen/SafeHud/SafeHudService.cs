using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;
using KludgeBox.Net.Packets;
using NeoVector;

namespace NeonWarfare;

[GamePacket]
public class ClientWantToBattlePacket : AbstractPacket;

[GameService]
public class SafeHudService
{

    [EventListener(ListenerSide.Server)]
    public void OnClientWantToBattlePacket(ClientWantToBattlePacket clientWantToBattlePacket)
    {
        NetworkOld.SendPacketToClients(new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Battle));
        BattleWorldMainScene battleWorldMainScene = Root.Instance.PackedScenes.Main.BattleWorld.Instantiate<BattleWorldMainScene>();
        Root.Instance.MainSceneContainer.ChangeStoredNode(battleWorldMainScene);
        BattleWorld battleWorld = battleWorldMainScene.World;
        
        Root.Instance.NetworkEntityManager.Clear();
        
        foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
        {
            Player player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));
            
            if (battleWorld.GetChild<Camera>() == null)
            {
                battleWorld.Player = player;
                
                var camera = new Camera(); //TODO del from server!!
                camera.Position = player.Position;
                camera.TargetNode = player;
                camera.Zoom = Vec(0.65);
                camera.SmoothingPower = 1.5;
                battleWorld.AddChild(camera);
                camera.Enabled = true;
            }

            playerServerInfo.Player = player;
            battleWorld.AddChild(player);
            long newPlayerNid = Root.Instance.NetworkEntityManager.AddEntity(player);
            
            NetworkOld.SendPacketToPeer(playerServerInfo.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (PlayerServerInfo allyServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                NetworkOld.SendPacketToPeer(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
    }
}