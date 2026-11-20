using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// What shows this process's player its World, owned by its starter. The World and the transport only report; the
/// UI is created by the owner, so that it never exists while the player is not online.
/// </summary>
public interface ILocalPlayerOwner
{
    /// <summary>
    /// The server has applied this process's join, and the player is online. Called on the host inside the tick, on
    /// a remote client from the events packet of its join tick.
    /// </summary>
    void Joined();

    /// <summary>
    /// The server refused this process's join. Called by the transport, never by the World: a remote client gets it
    /// before it has a World, the host inside the tick.
    /// </summary>
    void JoinRejected(JoinRejectReason reason);
}
