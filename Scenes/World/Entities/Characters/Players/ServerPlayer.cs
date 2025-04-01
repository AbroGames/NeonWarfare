using System.Linq;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer : ServerCharacter
{
    public ServerPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ServerPlayerProfile playerProfile)
    {
        PlayerProfile = playerProfile;
        
        Color = playerProfile.Color;
        foreach (var kv in playerProfile.SkillById)
        {
            AddSkill(kv.Key, kv.Value);
        }
        
        MaxHp = playerProfile.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = playerProfile.RegenHpSpeed;
        MovementSpeed = playerProfile.MovementSpeed;
        RotationSpeed = playerProfile.RotationSpeed;
    }

    public override void OnHit(double damage, ServerCharacter author, long authorPeerId)
    {
        base.OnHit(damage, author, authorPeerId);
        
        if (authorPeerId > 0 && !ServerRoot.Instance.Game.GameSettings.FriendlyFire) return;

        TakeDamage(damage, author);
        Network.SendToAll(new ClientAlly.SC_ChangeAllyStatsPacket(PlayerProfile.PeerId, Hp));
    }

    public override void OnHeal(double heal, ServerCharacter author, long authorPeerId)
    {
        base.OnHeal(heal, author, authorPeerId);
        
        if (authorPeerId == -1 && !ServerRoot.Instance.Game.GameSettings.HealPlayerByEnemy) return;
        
        TakeHeal(heal, author);
        Network.SendToAll(new ClientAlly.SC_ChangeAllyStatsPacket(PlayerProfile.PeerId, Hp));
    }
    
    public override void OnResurrect(double heal, ServerCharacter author, long authorPeerId)
    {
        base.OnResurrect(heal, author, authorPeerId);
        
        if (authorPeerId == -1 && !ServerRoot.Instance.Game.GameSettings.ResurrectPlayerByEnemy) return;
        
        if (!IsDead) return; //TODO убрать дублирование с Resurrect (+TODO внизу)
        Resurrect(heal, author);
        Network.SendToAll(new ClientAlly.SC_AllyResurrectionPacket(PlayerProfile.PeerId));
        Network.SendToAll(new ClientAlly.SC_ChangeAllyStatsPacket(PlayerProfile.PeerId, heal)); //TODO переделать так, чтобы в ServerPlayer можно было вызвать Heal внутри Resurrect и пакет бы сам отправился (+TODO вверху)
    }

    protected override void OnDeath()
    {
        Network.SendToAll(new ClientAlly.SC_AllyDeadPacket(PlayerProfile.PeerId));
    }
}
