using System.Numerics;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientAlly 
{

    public void OnChangeAllyStatsPacket(SC_ChangeAllyStatsPacket changeAllyStatsPacket)
    {
        Hp = changeAllyStatsPacket.Hp;
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnChangeAllyStatsPacketListener(SC_ChangeAllyStatsPacket changeAllyStatsPacket)
    {
        ClientRoot.Instance.Game.World.AlliesByPeerId[changeAllyStatsPacket.PeerId].OnChangeAllyStatsPacket(changeAllyStatsPacket);
    }
}
