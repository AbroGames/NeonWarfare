using System;
using NeonWarfare;

public class ClientPlayerProfile : ClientAllyProfile
{
    
    public ClientPlayer Player => ClientRoot.Instance.Game.World.Player;

    public ClientPlayerProfile(long peerId) : base(peerId) { }
}