using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The owner of this process's player, implemented by <c>Game</c>.
/// The World and the transport only report the outcome of the join.
/// The owner creates the UI itself, so the UI never exists while the player is not online.
/// </summary>
public interface ILocalPlayerOwner
{
    /// <summary>
    /// The server has applied this process's join, and the player is online.
    /// On the host it is called inside the tick.
    /// On a remote client it is called from the tick update of the join tick.
    /// </summary>
    void Joined();

    /// <summary>
    /// The server is disconnecting this process's player, before or after its join.
    /// The transport calls it, never the World, because a remote client may be disconnected before it has a World.
    /// </summary>
    void Disconnected(DisconnectReason reason);
}
