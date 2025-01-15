using System;
using NeonWarfare;

public class ClientPlayerProfile : ClientAllyProfile
{

    public ClientPlayer Player => (ClientPlayer) Ally;

    public ClientPlayerProfile(long peerId) : base(peerId) { }
}