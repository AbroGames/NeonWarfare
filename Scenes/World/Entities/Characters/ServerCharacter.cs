using System.Collections.Generic;
using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.Content.Skills.Impl;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.KludgeBox.Networking;
using NeonWarfare.Scripts.Utils.NetworkEntityManager;

namespace NeonWarfare.Scenes.World.Entities.Characters;

public partial class ServerCharacter : CharacterBody2D
{
    [Export] [NotNull] public CollisionShape2D CollisionShape { get; private set; }
    [Export] [NotNull] public Area2D HitBox { get; private set; }

    public long Nid => this.GetChild<NetworkEntityComponent>().Nid;
	
    public Color Color { get; protected set; } //Цвет, который будет использоваться для снарядов и т.п.
    public double MaxHp { get; set; }
    public double Hp { get; set; }
    public double RegenHpSpeed { get; set; }
    public double MovementSpeed { get; set; }
    public double RotationSpeed { get; set; }

    public record SkillInfo(string SkillType, double Cooldown, double DamageFactor, double SpeedFactor, double RangeFactor);
    private Dictionary<long, SkillInfo> _skillById  = new Dictionary<long, SkillInfo>();
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
        
        //TODO del after test
        _skillById[0] = new SkillInfo("DefaultShot", 0, 1, 1 ,1);
        _skillById[1] = new SkillInfo("Shotgun", 0, 1, 1 ,1);
    }

    public virtual void OnHit(double damage, ServerCharacter author, long authorPeerId) { }

    public void TakeDamage(double damage, ServerCharacter author)
    {
        Hp -= damage;
        if (Hp <= 0)
        {
            OnDeath(author);
        }
    }

    public virtual void OnDeath(ServerCharacter killer)
    {
        killer.OnKill(this);
        QueueFree();
    }

    public virtual void OnKill(ServerCharacter dead)
    {
        
    }
    
    public void UseSkill(long skillId, Vector2 characterPosition, float characterRotation, Vector2 cursorGlobalPosition, long authorPeerId)
    {
        SkillInfo skillInfo = _skillById[skillId];
        Skill skill = SkillStorage.GetSkill(skillInfo.SkillType);
        skill.OnServerUse(new Skill.ServerSkillUseInfo(
            World: ServerRoot.Instance.Game.World,
            CharacterPosition: characterPosition,
            CharacterRotation: characterRotation,
            CursorGlobalPosition: cursorGlobalPosition,
            Author: this, 
            AuthorPeerId: authorPeerId,
            DamageFactor: skillInfo.DamageFactor,
            SpeedFactor: skillInfo.SpeedFactor,
            RangeFactor: skillInfo.RangeFactor
            ));
    }
}
