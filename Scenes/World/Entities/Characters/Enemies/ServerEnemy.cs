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
    [Export] [NotNull] public NavigationAgent2D NavigationAgent { get; private set; }

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

        var enemyAi = new ServerEnemyAiComponent();
        AddChild(enemyAi);
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

    /// <summary>
    /// Эта штука должна вернуть радиус юнита в контексте поиска пути. В радиус этой окружности должно вписываться физическое тело врага (не хитбокс).
    /// По умолчанию пытается получить радиус окружности, вписанной в ограничительную рамку физической формы (ты еще читаешь?) юнита. На выходе должна получаться исходная окружность, наверное.
    /// </summary>
    /// <returns>Примерный размер юнита</returns>
    /// <remarks>
    /// TODO: Жыамбек, ты что-то говорил про виртуальный метод с размерами или чет такое. Вот, переопределяй если надо.
    /// </remarks>
    public virtual float GetNavigationRadius()
    {
        return CollisionShape.Shape.GetRect().Size.X // ширина прямоугольника, описанного вокруг формы, БЕЗ учета Scale
            / 2 // половина этой ширины будет равна радиусу окружности
            * 1.05f; // небольшой запасик на всякий случай
    }

    /// <summary>
    /// Этот метод должен вернуть дальность зрения или внимания врага, в пределах которого он должен повернуться к своей цели
    /// </summary>
    /// <returns>Радиус поля зрения врага</returns>
    /// <remarks>
    /// TODO: Жижа, этот метод тоже для тебя, пользуйся
    /// </remarks>
    public virtual float GetEnemyReachRange()
    {
        return 2000; // Самое большое значение, которое я нашел в скиллах
    }

    /// <summary>
    /// Назначает маску поиска пути для NavigationAgent
    /// </summary>
    /// <param name="layersBitMask"></param>
    public void AssignNavigationLayers(uint layersBitMask)
    {
        NavigationAgent.SetNavigationLayers(layersBitMask);
    }
}
