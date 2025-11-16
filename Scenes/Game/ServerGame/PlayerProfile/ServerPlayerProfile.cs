using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare;
using NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;

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
    
    public bool ChangeClass(string className) {
        if (className.Equals("def")) InitStats();
        else if (className.Equals("tank")) InitStatsTank();
        else if (className.Equals("dd")) InitStatsDd();
        else if (className.Equals("heal")) InitStatsHeal();
        else return false;
        
        //Отправляем всем инфу о характеристиках нового игрока
        Network.SendToAll(new ClientAllyProfile.SC_ChangeAllyProfilePacket(PeerId, MaxHp, RegenHpSpeed, MovementSpeed, RotationSpeed));
        //Отправляем игроку его скиллы
        foreach (var kv in SkillById)
        {
            Network.SendToClient(PeerId, new ClientPlayerProfile.SC_ChangeSkillPlayerProfilePacket(kv.Key, kv.Value.SkillType, kv.Value.Cooldown));
        }

        //Респавним игрока
        //ServerRoot.Instance.Game.World.PlayersByPeerId[PeerId].Free();
        //ServerRoot.Instance.Game.World.SpawnPlayerInCenter(this);
        
        return true;
    }
    
    //Указаны дефолтные значения, которые имеет игрок при начале игры
    public void InitStats()
    {
        MaxHp = 300;
        RegenHpSpeed = 10;
        MovementSpeed = 240;
        RotationSpeed = 300;
        
        _skillById.Clear();
        AddSkill(new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.5, 1.2, 1.2, 1));
        AddSkill(new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 0.5, 1.2, 1.2, 1));
        AddSkill(new ServerCharacter.SkillInfo(SelfHealSkill.SkillTypeConst, 20, 6, 1, 1));
        AddSkill(new ServerCharacter.SkillInfo(ResurrectShotSkill.SkillTypeConst, 17.5, 1, 1, 1));
    }
    
    public void InitStatsTank()
    {
        MaxHp = 800;
        RegenHpSpeed = 20;
        MovementSpeed = 210;
        RotationSpeed = 200;
        
        _skillById.Clear();
        AddSkill(new ServerCharacter.SkillInfo(ShotgunSkill.SkillTypeConst, 0.5, 2, 1.5, 0.6));
        AddSkill(new ServerCharacter.SkillInfo(ResurrectShotSkill.SkillTypeConst, 30, 1, 1, 1));
        AddSkill(new ServerCharacter.SkillInfo(SelfHealSkill.SkillTypeConst, 40, 16, 1, 1));
        AddSkill(new ServerCharacter.SkillInfo(SelfHealSkill.SkillTypeConst, 40, 16, 1, 1));
    }
    
    public void InitStatsDd()
    {
        MaxHp = 100;
        RegenHpSpeed = 3;
        MovementSpeed = 250;
        RotationSpeed = 300;
        
        _skillById.Clear();
        AddSkill(new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.5, 0.75, 1.3, 0.8));
        AddSkill(new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 6, 20, 1.8, 2.5));
        AddSkill(new ServerCharacter.SkillInfo(HealShotSkill.SkillTypeConst, 5, 1, 1, 1));
        AddSkill(new ServerCharacter.SkillInfo(ResurrectShotSkill.SkillTypeConst, 30, 1, 1, 1));
    }
    
    public void InitStatsHeal()
    {
        MaxHp = 60;
        RegenHpSpeed = 2;
        MovementSpeed = 400;
        RotationSpeed = 400;
        
        _skillById.Clear();
        AddSkill(new ServerCharacter.SkillInfo(DefaultShotSkill.SkillTypeConst, 0.75, 1, 1, 2));
        AddSkill(new ServerCharacter.SkillInfo(HealShotSkill.SkillTypeConst, 0.5, 0.5, 1.7, 1.5));
        AddSkill(new ServerCharacter.SkillInfo(HealShotSkill.SkillTypeConst, 6, 8, 0.2, 1));
        AddSkill(new ServerCharacter.SkillInfo(ResurrectShotSkill.SkillTypeConst, 7.5, 4, 2, 2));
    }

    public void AddSkill(ServerCharacter.SkillInfo skill)
    {
        long newSkillId = _skillById.Count == 0 ? 0 : _skillById.Keys.Max() + 1;
        _skillById.Add(newSkillId, skill);
    }
}
