using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The player of this process, as it joins the World: one object both for the join request and for the World,
/// so the World knows exactly the uid that was sent. <c>null</c> on a dedicated server.
/// </summary>
/// <param name="Nick">What the player asks for on join: a returning player keeps the stored nick and color, so
/// they are read from <c>PlayerModel</c>, not from here.</param>
public record LocalPlayer(string Uid, string Nick, Color Color)
{
    public JoinRequestCommand ToJoinRequest(ulong protocolHash) => new(protocolHash, Uid, Nick, Color);
}
