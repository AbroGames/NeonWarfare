using System;
using NeonWarfare;

namespace NeonWarfare.Scenes.Game.ClientGame.AllyProfile;

public class ClientPlayerProfile : ClientAllyProfile
{

    public ClientPlayer Player => (ClientPlayer) Ally;

    public ClientPlayerProfile(long peerId) : base(peerId) { }
}
