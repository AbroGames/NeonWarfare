using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content.Skills.Impl;

namespace NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;

public partial class ServerPlayerProfile
{
    public long PeerId { get; }
    public long UserId { get; set; }
    public bool IsAdmin { get; set; }
    
    public string Name { get; set; }
    public Color Color { get; set; }
    
    public IReadOnlyDictionary<long, ServerCharacter.SkillInfo> SkillById => _skillById;
    private Dictionary<long, ServerCharacter.SkillInfo> _skillById = new();
    
    public double MaxHp { get; set; }
    public double RegenHpSpeed { get; set; } // hp/sec
    public double MovementSpeed { get; set; } // in pixels/sec
    public double RotationSpeed { get; set; } // in degree/sec

    public ServerPlayerProfile(long peerId)
    {
        PeerId = peerId;
        InitStats();
    }

    //Указаны дефолтные значения, которые имеет игрок при начале игры
    public void InitStats()
    {
        MaxHp = 100;
        RegenHpSpeed = 1;
        MovementSpeed = 250;
        RotationSpeed = 300;
        
        AddSkill(new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.5, 1, 1, 1));
        AddSkill(new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 5, 5, 1, 1));
    }

    public void AddSkill(ServerCharacter.SkillInfo skill)
    {
        long newSkillId = _skillById.Count == 0 ? 0 : _skillById.Keys.Max() + 1;
        _skillById.Add(newSkillId, skill);
    }
}
