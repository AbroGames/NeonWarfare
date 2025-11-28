namespace NeonWarfare.Scenes.NeonTemp.Entity.Character.Stats;

public enum CharacterStat {
    MaxHp, // hp
    RegenHp, // hp/sec [0;..]
    DrainHp, // hp/sec [0;..]
    Armor, // hp
        
    MovementSpeed, // px/sec
    RotationSpeed, // deg/sec
    Mass, // kg
        
    SkillRange, // px
    SkillDamage, // hp
    SkillHeal, // hp
    SkillSpeed, // px/sec (projectile)
    SkillCooldown, // sec
        
    SkillCritChance, // [0;1] percent
    SkillCritModifier // [0;1+] overdamage/overheal percent
}