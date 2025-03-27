using System.Numerics;
using Godot;
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
        if (this is ClientPlayer 
            && changeAllyStatsPacket.Hp < Hp 
            && changeAllyStatsPacket.Hp <= AchievementsPrerequisites.LuckyDevil_HpLeft 
            && changeAllyStatsPacket.Hp > 0)
        {
            ClientRoot.Instance.UnlockAchievement(AchievementIds.LuckyDevil);
        }
        
        Hp = changeAllyStatsPacket.Hp;
    }
    
    public void OnAllyDeadPacket(SC_AllyDeadPacket allyDeadPacket)
    {
        IsDead = true;
        Sprite.Modulate = new Color(0.5f, 0.5f, 0.5f);
    }
    
    public void OnAllyResurrectionPacket(SC_AllyResurrectionPacket allyResurrectionPacket)
    {
        IsDead = false;
        Sprite.Modulate = AllyProfile.Color;
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnChangeAllyStatsPacketListener(SC_ChangeAllyStatsPacket changeAllyStatsPacket)
    {
        ClientRoot.Instance.Game.World.AlliesByPeerId[changeAllyStatsPacket.PeerId].OnChangeAllyStatsPacket(changeAllyStatsPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnAllyDeadPacketListener(SC_AllyDeadPacket allyDeadPacket)
    {
        ClientRoot.Instance.Game.World.AlliesByPeerId[allyDeadPacket.PeerId].OnAllyDeadPacket(allyDeadPacket);
    }
    
    [EventListener(ListenerSide.Client)]
    public static void OnAllyResurrectionPacketListener(SC_AllyResurrectionPacket allyResurrectionPacket)
    {
        ClientRoot.Instance.Game.World.AlliesByPeerId[allyResurrectionPacket.PeerId].OnAllyResurrectionPacket(allyResurrectionPacket);
    }
}
