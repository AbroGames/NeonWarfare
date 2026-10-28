using System;

namespace NeonWarfare.Scenes.World.ServerNetwork;

/// <summary>
/// The transport to the clients, owned by <c>Game</c>. A packet for the host's own peer never reaches the network:
/// <c>Game</c> hands it to its own World synchronously, so the World has no loopback of its own.
/// No queue: the host's <c>Handle</c> must run in the physics step, before <c>_Process</c>. Then whatever the
/// Presentation puts into <c>HudMailbox</c> is read by the HUD in the same frame, as on a remote client, where the
/// packet is handled in <c>poll</c>, also before <c>_Process</c>.
/// </summary>
public interface IClientsConnection
{
    /// <summary>
    /// Reliable and ordered. The bytes are valid only during the call: the sender reuses the buffer for the next
    /// peer, so an implementation that sends or loops back later must copy them first.
    /// </summary>
    void Send(int peerId, ReadOnlySpan<byte> packet);
}
