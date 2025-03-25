using Godot;
using NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Networking;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ServerPlayer : ServerCharacter
{
    public ServerPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ServerPlayerProfile playerProfile)
    {
        PlayerProfile = playerProfile;
        
        MaxHp = playerProfile.MaxHp;
        Hp = MaxHp;
        RegenHpSpeed = playerProfile.RegenHpSpeed;
        MovementSpeed = playerProfile.MovementSpeed;
        RotationSpeed = playerProfile.RotationSpeed;
    }
}
