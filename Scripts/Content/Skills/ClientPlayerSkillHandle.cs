using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;
using NeonWarfare.Scripts.Utils.Cooldown;

namespace NeonWarfare.Scripts.Content.Skills;

public class ClientPlayerSkillHandle
{
    public ClientPlayer Player { get; }
    public Skill Skill { get; }
    public long SkillId { get; }
    public string Key { get; }
    
    
    private readonly ManualCooldown _cooldown;

    public ClientPlayerSkillHandle(ClientPlayer player, Skill skill, long skillId)
    {
        Player = player;
        Skill = skill;
        SkillId = skillId;
        
        _cooldown = Player.GetCooldownById(SkillId);
        Key = GetActionKey(player.GetActionNameById(skillId));
    }

    public Texture2D GetIcon() => SkillIconProvider.GetSkillIcon(Skill.SkillType);

    public bool CanUse()
    {
        return _cooldown.IsCompleted;
    }
    
    public double GetCooldownProgress()
    {
        return _cooldown.FractionElapsedTime;
    }

    public double GetCooldownDuration()
    {
        return _cooldown.Duration;
    }
    
    private string GetActionKey(StringName actionName)
    {
        var events = InputMap.ActionGetEvents(actionName);
        
        foreach (var inputEvent in events)
        {
            if (inputEvent is InputEventKey keyEvent)
            {
                return OS.GetKeycodeString(keyEvent.PhysicalKeycode);
            }
        }
        
        return ""; // If no key is found
    }
}