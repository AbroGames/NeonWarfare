using Godot;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Characters;

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
    public virtual void CheckEnemyUse() {}
}