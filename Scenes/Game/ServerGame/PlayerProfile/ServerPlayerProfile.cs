using Godot;
using NeonWarfare;
using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ServerGame.PlayerProfile;

public partial class ServerPlayerProfile
{
    public long PeerId { get; }
    public long UserId { get; set; }
    public bool IsAdmin { get; set; }
    
    public string Name { get; set; }
    public Color Color { get; set; }

    public ServerPlayer Player {get; set;}

    public ServerPlayerProfile(long peerId)
    {
        PeerId = peerId;
    }
}
