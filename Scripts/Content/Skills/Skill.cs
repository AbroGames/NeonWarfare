using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Characters;
using NeonWarfare.Scenes.World.Entities.Characters.Enemies;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;

namespace NeonWarfare.Scripts.Content.Skills;

public abstract class Skill
{
    public readonly string SkillType;

    protected Skill(string skillType)
    {
        SkillType = skillType;
    }

    public record ServerSkillUseInfo(ServerWorld World, Vector2 CharacterPosition, float CharacterRotation, Vector2 CursorGlobalPosition,
        ServerCharacter Author, long AuthorPeerId,
        double DamageFactor, double SpeedFactor, double RangeFactor);
    
    public record ClientSkillUseInfo(ClientWorld World, long Nid,
        Vector2 CharacterPosition, float CharacterRotation, Vector2 CursorGlobalPosition,
        Color Color, string CustomParams);
    
    public virtual void OnServerUse(ServerSkillUseInfo useInfo) {}
    public virtual void OnClientUse(ClientSkillUseInfo useInfo) {}
    public virtual bool CheckEnemyCanUse(ServerEnemy enemy, double rangeFactor) { return false; }

    protected bool CheckEnemyRayCastAndDistToTarget(ServerEnemy enemy, double range)
    {
        if (range > enemy.RayCast.TargetPosition.Length())
        {
            Log.Warning($"Too many enemy range in skill. SkillType = {SkillType}, RayCast lenght = {enemy.RayCast.TargetPosition.Length()}, Skill range = {range}");
        }
        
        Node2D target = enemy.GetChild<ServerEnemyTargetComponent>().Target;
        return enemy.RayCast.GetCollider() == target &&
               target != null &&
               enemy.DistanceTo(target) < range;
    }
}