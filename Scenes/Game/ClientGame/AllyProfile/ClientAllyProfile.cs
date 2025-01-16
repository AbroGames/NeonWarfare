using System;
using NeonWarfare;
using NeonWarfare.Scenes.World.Entities.Characters.Players;

namespace NeonWarfare.Scenes.Game.ClientGame.AllyProfile;

public class ClientAllyProfile
{
    public long PeerId { get; } 
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ClientAlly Ally { get; set; }

    public ClientAllyProfile(long peerId)
    {
        PeerId = peerId;
    }
}
