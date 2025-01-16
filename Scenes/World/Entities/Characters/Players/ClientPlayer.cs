using NeonWarfare.Scenes.Game.ClientGame.AllyProfile;

namespace NeonWarfare.Scenes.World.Entities.Characters.Players;

public partial class ClientPlayer : ClientAlly 
{
    
    public ClientPlayerProfile PlayerProfile { get; private set; }
    
    public void InitOnProfile(ClientPlayerProfile playerProfile)
    {
        base.InitOnProfile(playerProfile);
        PlayerProfile = playerProfile;
    }
}
