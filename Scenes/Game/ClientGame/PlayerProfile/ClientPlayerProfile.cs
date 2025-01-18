using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientPlayerProfile : ClientAllyProfile
{

    public ClientPlayer Player => (ClientPlayer) Ally;

    public ClientPlayerProfile(long peerId) : base(peerId) { }
}
