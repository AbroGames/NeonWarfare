using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientPlayerProfile : ClientAllyProfile
{

    public record ClientProfileSkillInfo(string SkillType, double Cooldown, StringName ActionToActivate);
    public Dictionary<long, ClientProfileSkillInfo> SkillById = new();
    
    public ClientPlayerProfile(long peerId) : base(peerId) { }
}
