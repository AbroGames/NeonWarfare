using System;

namespace NeonWarfare.Scenes.World.Infra.ClientNetwork;

/// <summary>
/// The transport to the server, owned by <c>Game</c>. On the host <c>Game</c> hands the packet to its own World
/// synchronously, as a packet from the host's own peer, so the host's commands pass the same decoding and whitelist
/// as a remote client's.
/// </summary>
public interface IServerConnection
{
    /// <summary>
    /// Reliable and ordered. The bytes are valid only during the call, so an implementation that sends or loops back
    /// later must copy them first.
    /// </summary>
    void Send(ReadOnlySpan<byte> packet);
}
