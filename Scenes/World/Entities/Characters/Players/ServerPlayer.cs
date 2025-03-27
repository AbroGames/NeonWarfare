using Godot;
using NeonWarfare.Scenes.Game.ClientGame;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scenes.Root.ServerRoot;
using NeonWarfare.Scenes.World.SafeWorld.ServerSafeWorld;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer : ServerCharacter
{
    public ServerPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ServerPlayerProfile playerProfile)
    {
        PlayerProfile = playerProfile;
        
        Color = playerProfile.Color;
        MaxHp = playerProfile.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = playerProfile.RegenHpSpeed;
        MovementSpeed = playerProfile.MovementSpeed;
        RotationSpeed = playerProfile.RotationSpeed;
    }

    public override void OnHit(double damage, ServerCharacter author, long authorPeerId)
    {
        base.OnHit(damage, author, authorPeerId);
        
        if (author == this) return;
        if (authorPeerId > 0 && !ServerRoot.Instance.Game.FriendlyFire) return;

        TakeDamage(damage, author);
        Network.SendToAll(new ClientAlly.SC_ChangeAllyStatsPacket(PlayerProfile.PeerId, Hp));
    }
}
