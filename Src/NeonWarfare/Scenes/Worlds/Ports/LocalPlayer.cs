using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The player of this process as it joins the World.
/// The join request is built from the same object, so the World knows exactly the uid that was sent.
/// </summary>
/// <param name="Nick">The nick the player asks for on join.
/// A returning player keeps the stored nick and color, so read them from <c>PlayerModel</c>, not from here.</param>
public record LocalPlayer(PlayerUid Uid, string Nick, Color Color)
{
    public JoinRequestCommand ToJoinRequest(ProtocolHash protocolHash) => new(protocolHash.Value, Uid, Nick, Color);
}
