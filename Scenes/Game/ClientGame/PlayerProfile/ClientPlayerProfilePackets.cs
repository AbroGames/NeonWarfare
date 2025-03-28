using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientPlayerProfile 
{
    
    [GamePacket]
    public class SC_ChangeSkillPlayerProfilePacket(long skillId, string skillType, double cooldown) : BinaryPacket
    {
        public long SkillId = skillId;
        public string SkillType = skillType;
        public double Cooldown = cooldown;
    }
}
