using System;
using System.Collections.Generic;
using System.Linq;
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
using NeonWarfare.Scripts.Utils.Cooldown;
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
    
    public bool IsDead { get; protected set; }
    
    public record SkillWithCooldown(SkillInfo SkillInfo, ManualCooldown Cooldown);
    public record SkillInfo(string SkillType, double Cooldown, double DamageFactor, double SpeedFactor, double RangeFactor);
    public IReadOnlyDictionary<long, SkillWithCooldown> SkillById => _skillById;
    private Dictionary<long, SkillWithCooldown> _skillById  = new();
    
    public override void _Ready()
    {
        NotNullChecker.CheckProperties(this);
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (!IsDead)
        {
            Hp = Math.Min(Hp + delta * RegenHpSpeed, MaxHp);
        }
        foreach (var skillCooldown in _skillById.Values.Select(skill => skill.Cooldown))
        {
            skillCooldown.Update(delta);
        }
    }

    public virtual void OnHit(double damage, ServerCharacter author, long authorPeerId) { }
    
    public virtual void OnHeal(double heal, ServerCharacter author, long authorPeerId) { }

    public void TakeDamage(double damage, ServerCharacter author)
    {
        if (IsDead) return;
        Hp -= damage;
        if (Hp <= 0)
        {
            Hp = 0;
            Death(author);
        }
    }

    public void TakeHeal(double heal, ServerCharacter author)
    {
        if (IsDead) return;
        Hp = Math.Min(Hp + heal, MaxHp);
    }

    public void Death(ServerCharacter killer)
    {
        IsDead = true;
        killer.AsKiller(this);
        OnDeath();
    }

    protected virtual void OnDeath()
    {
        QueueFree();
    }

    public void Resurrect(double hp, ServerCharacter author)
    {
        if (!IsDead) return;
        IsDead = false;
        Hp = hp;
        author.AsResurrector(this);
    }
    
    protected virtual void OnResurrect()
    {
        
    }

    protected virtual void AsKiller(ServerCharacter dead) { }
    protected virtual void AsResurrector(ServerCharacter dead) { }
    
    public void AddSkill(long skillId, SkillInfo skill)
    {
        _skillById.Add(skillId, new SkillWithCooldown(skill, new ManualCooldown(skill.Cooldown, true)));
    }
    
    public void AddSkill(SkillInfo skill)
    {
        long newSkillId = _skillById.Count == 0 ? 0 : _skillById.Keys.Max() + 1;
        AddSkill(newSkillId, skill);
    }

    public void TryUseSkill(long skillId, Vector2 characterPosition, float characterRotation, Vector2 cursorGlobalPosition, long authorPeerId)
    {
        if (!_skillById[skillId].Cooldown.IsCompleted) return;
        
        _skillById[skillId].Cooldown.Restart();
        UseSkill(skillId, characterPosition, characterRotation, cursorGlobalPosition, authorPeerId);
    }
    
    public void UseSkill(long skillId, Vector2 characterPosition, float characterRotation, Vector2 cursorGlobalPosition, long authorPeerId)
    {
        SkillInfo skillInfo = _skillById[skillId].SkillInfo;
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
