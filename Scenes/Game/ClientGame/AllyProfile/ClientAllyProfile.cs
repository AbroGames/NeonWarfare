using System;
using NeonWarfare;

public class ClientAllyProfile
{
    public long PeerId { get; private set; } 
    public long UserId { get; set; }
    public string Nickname { get; set; } = "";
    public bool IsAdmin { get; set; } = false;

    public ClientAlly Ally => ClientRoot.Instance.Game.World.AlliesByPeerId[PeerId];

    public ClientAllyProfile(long peerId)
    {
        PeerId = peerId;
    }
}