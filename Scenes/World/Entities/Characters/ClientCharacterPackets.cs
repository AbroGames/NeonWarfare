using System;
using Godot;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ClientCharacter 
{
    [GamePacket]
    public class SC_DamageCharacterPacket(long nid, long authorPeerId, string skillType, double damage, bool targetDied) : BinaryPacket, IInstanceEvent
    {
        public object NetworkId => Nid;
        
        public long Nid = nid;
        public long AuthorPeerId = authorPeerId;
        public string SkillType = skillType;
        public double Damage = damage;
        public bool TargetDied = targetDied;
    }
    
    [GamePacket]
    public class SC_HealCharacterPacket(long nid, long authorPeerId, string skillType, double heal) : BinaryPacket, IInstanceEvent
    {
        public object NetworkId => Nid;
        
        public long Nid = nid;
        public long AuthorPeerId = authorPeerId;
        public string SkillType = skillType;
        public double Heal = heal;
    }
    
    [GamePacket]
    public class SC_ResurrectCharacterPacket(long nid, long authorPeerId, string skillType, double heal) : BinaryPacket, IInstanceEvent
    {
        public object NetworkId => Nid;
        
        public long Nid = nid;
        public long AuthorPeerId = authorPeerId;
        public string SkillType = skillType;
        public double Heal = heal;
    }
}
