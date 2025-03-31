using Godot;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.Content.Skills;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Components;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scenes.World.Entities.Characters.Enemies;

public partial class ServerEnemy : ServerCharacter
{
    [Export] [NotNull] public RayCast2D RayCast { get; private set; }

    public void InitComponents()
    {
        ServerEnemyTargetComponent serverEnemyTargetComponent = new ServerEnemyTargetComponent();
        AddChild(serverEnemyTargetComponent);
        
        AddChild(new ServerEnemyMovementComponent());
        
        RotateComponent rotateComponent = new RotateComponent();
        rotateComponent.GetTargetGlobalPositionFunc = () =>
        {
            if (serverEnemyTargetComponent.Target == null || !serverEnemyTargetComponent.Target.IsValid()) return null;
            return serverEnemyTargetComponent.Target.GlobalPosition;
        };
        rotateComponent.GetRotationSpeedFunc = () => RotationSpeed;
        AddChild(rotateComponent);
    }

    public void InitOnSpawn(Vector2 position, float rotation)
    {
        Position = position;
        Rotation = rotation;
    }

    public void InitStats(EnemyInfoStorage.EnemyInfo enemyInfo)
    {
        Color = enemyInfo.Color;
        enemyInfo.Skills.ForEach(AddSkill);
        
        MaxHp = enemyInfo.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = enemyInfo.RegenHpSpeed;
        MovementSpeed = enemyInfo.MovementSpeed;
        RotationSpeed = enemyInfo.RotationSpeed;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        foreach (var kv in SkillById)
        {
            if (SkillStorage.GetSkill(kv.Value.SkillInfo.SkillType).CheckEnemyCanUse(this, kv.Value.SkillInfo.RangeFactor))
            {
                TryUseSkill(kv.Key);
            }
        }
    }

    public override void OnHit(double damage, ServerCharacter author, long authorPeerId)
    {
        base.OnHit(damage, author, authorPeerId);
        
        if (author == this) return;
        if (authorPeerId == -1 && !ServerRoot.Instance.Game.EnemyFriendlyFire) return;
        
        TakeDamage(damage, author);
    }

    public override void OnHeal(double heal, ServerCharacter author, long authorPeerId)
    {
        base.OnHeal(heal, author, authorPeerId);
        
        if (authorPeerId > 0 && !ServerRoot.Instance.Game.HealEnemyByPlayer) return;
        
        TakeHeal(heal, author);
    }
    
    public override void OnResurrect(double heal, ServerCharacter author, long authorPeerId)
    {
        base.OnResurrect(heal, author, authorPeerId);
        
        if (authorPeerId > 0 && !ServerRoot.Instance.Game.ResurrectEnemyByPlayer) return;
        
        Resurrect(heal, author);
    }

    public void TryUseSkill(long skillId)
    {
        TryUseSkill(skillId, Position, Rotation, GetGlobalMousePosition(), -1);
    }
}
