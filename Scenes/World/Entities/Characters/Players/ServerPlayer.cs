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
        playerProfile.Player = this;
        //TODO MaxHp = maxHp и т.д, у клиента аналогично
    }
}
