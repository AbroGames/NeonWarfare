using Godot;
using KludgeBox;
using KludgeBox.Events;
using KludgeBox.Net;

namespace NeoVector;

[GameService]
public class SafeHudService
{
    
    [EventListener(ListenerSide.Client)]
    public void OnSafeHudProcessEvent(SafeHudProcessEvent safeHudProcessEvent)
    {
        SafeHud safeHud = safeHudProcessEvent.SafeHud;
        SafeWorld safeWorld = safeHud.SafeWorld;
        Player player = safeWorld.Player;
        if (player == null) return;

        safeHud.HpBar.CurrentUpperValue = player.Hp;
        safeHud.HpBar.CurrentLowerValue = player.Hp; //TODO сделать аналогично с BattleHud, вынести в общий сервис (не дублировать код)
        safeHud.HpBar.MaxValue = player.MaxHp;
        safeHud.HpBar.Label.Text = $"Health: {player.Hp:N0} / {player.MaxHp:N0}";
        safeHud.Fps.Text = $"FPS: {Engine.GetFramesPerSecond():N0}";
    }

    [EventListener(ListenerSide.Client)]
    public void OnToBattleButtonClickEvent(ToBattleButtonClickEvent toBattleButtonClickEvent)
    {
        Network.SendPacketToServer(new ClientWantToBattlePacket());
    }

    [EventListener(ListenerSide.Server)]
    public void OnClientWantToBattlePacket(ClientWantToBattlePacket clientWantToBattlePacket)
    {
        Network.SendPacketToClients(new ServerChangeWorldPacket(ServerChangeWorldPacket.ServerWorldType.Battle));
        BattleWorldMainScene battleWorldMainScene = Root.Instance.PackedScenes.Main.BattleWorld.Instantiate<BattleWorldMainScene>();
        Root.Instance.MainSceneContainer.ChangeStoredNode(battleWorldMainScene);
        
        Root.Instance.NetworkEntityManager.Clear();
        
        foreach (PlayerServerInfo playerServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
        {
            Player player = Root.Instance.PackedScenes.World.Player.Instantiate<Player>();
            player.Position = Vec(Rand.Range(-100, 100), Rand.Range(-100, 100));
            player.Rotation = Mathf.DegToRad(Rand.Range(0, 360));

            playerServerInfo.Player = player;
            battleWorldMainScene.WorldContainer.GetCurrentStoredNode<BattleWorld>().AddChild(player);
            long newPlayerNid = Root.Instance.NetworkEntityManager.AddEntity(player);
            
            Network.SendPacketToPeer(playerServerInfo.Id, 
                new ServerSpawnPlayerPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));

            foreach (PlayerServerInfo allyServerInfo in Root.Instance.Server.PlayerServerInfo.Values)
            {
                if (allyServerInfo.Id == playerServerInfo.Id) continue;
                Network.SendPacketToPeer(allyServerInfo.Id, new ServerSpawnAllyPacket(newPlayerNid, player.Position.X, player.Position.Y, player.Rotation));
            }
        }
    }
}