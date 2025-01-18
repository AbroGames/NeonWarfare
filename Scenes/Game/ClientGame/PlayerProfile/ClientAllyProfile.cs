using Godot;
using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ClientGame.PlayerProfile;

public partial class ClientAllyProfile
{
    public long PeerId { get; } 
    public long UserId { get; set; }
    public bool IsAdmin { get; set; }
    
    public string Name { get; set; }
    public Color Color { get; set; }

    public ClientAlly Ally { get; set; }

    public ClientAllyProfile(long peerId)
    {
        PeerId = peerId;
    }
}
