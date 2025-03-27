using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Entities.Characters;

namespace NeonWarfare.Scripts.Content.Skills.Impl;

public class ShotgunSkill() : Skill("Shotgun")
{
    private const ActionInfoStorage.ActionType ActionType = ActionInfoStorage.ActionType.Shot; 
    
    public override void OnServerUse(ServerSkillUseInfo useInfo)
    {
        
    }

    public override void OnClientUse(ClientSkillUseInfo useInfo)
    {
        
    }

    public override void CheckEnemyUse()
    {
        
    }
}