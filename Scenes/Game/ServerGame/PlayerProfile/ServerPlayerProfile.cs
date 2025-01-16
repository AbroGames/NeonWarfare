using NeonWarfare;
using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;

public class ServerPlayerProfile
{
    public long PeerId { get; }
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ServerPlayer Player {get; set;}

    public ServerPlayerProfile(long peerId)
    {
        PeerId = peerId;
    }
}
