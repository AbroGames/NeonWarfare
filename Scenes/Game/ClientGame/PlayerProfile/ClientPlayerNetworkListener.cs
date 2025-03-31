using System.Collections.Generic;
using System.Linq;
using Godot;
using NeonWarfare.Scenes.Game.ClientGame.MainScenes;
using NeonWarfare.Scenes.Root.ClientRoot;
using NeonWarfare.Scripts.Content;
using NeonWarfare.Scripts.KludgeBox.Events;
namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientPlayerProfile
{
    
    public void OnChangeSkillPlayerProfilePacket(SC_ChangeSkillPlayerProfilePacket changeSkillPlayerProfilePacket)
    {
        StringName skillActionKey = null;

        if (SkillById.ContainsKey(changeSkillPlayerProfilePacket.SkillId))
        {
            skillActionKey = SkillById[changeSkillPlayerProfilePacket.SkillId].ActionToActivate;
        }
        else
        {
            List<StringName> skillActionKeys = [Keys.AttackPrimary, Keys.AttackSecondary, Keys.AbilityBasic, Keys.AbilityAdvanced];
            List<StringName> currentSkillActionKeys = SkillById.Values.Select(skill => skill.ActionToActivate).ToList();
            List<StringName> freeButtons = skillActionKeys.Except(currentSkillActionKeys).ToList();
            if (freeButtons.Count > 0)
            {
                skillActionKey = freeButtons[0];
            }
        }
        
        SkillById[changeSkillPlayerProfilePacket.SkillId] = new ClientProfileSkillInfo(
            changeSkillPlayerProfilePacket.SkillType, 
            changeSkillPlayerProfilePacket.Cooldown,
            skillActionKey);
    }
    
    /*
     * Изменяем скилл игроку
     */
    [EventListener(ListenerSide.Client)]
    public static void OnChangeSkillPlayerProfilePacketListener(SC_ChangeSkillPlayerProfilePacket changeSkillPlayerProfilePacket)
    {
        ClientRoot.Instance.Game.PlayerProfile.OnChangeSkillPlayerProfilePacket(changeSkillPlayerProfilePacket);
    }
}
