using System.Numerics;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Events;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer 
{
    
    public void OnUseSkillPacket(CS_UseSkillPacket useSkillPacket)
    {
        UseSkill(useSkillPacket.SkillId, useSkillPacket.PlayerPosition, useSkillPacket.PlayerRotation, useSkillPacket.CursorGlobalPosition, useSkillPacket.SenderId);
    }

    [EventListener(ListenerSide.Server)]
    public static void OnUseSkillPacketListener(CS_UseSkillPacket useSkillPacket)
    {
        ServerRoot.Instance.Game.World.PlayersByPeerId[useSkillPacket.SenderId].OnUseSkillPacket(useSkillPacket);
    }
}
