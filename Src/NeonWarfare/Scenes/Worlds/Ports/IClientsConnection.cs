using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The connection to the clients, implemented by a transport of <c>Game</c>. The World hands over bodies; how a packet
/// says what it carries is the transport's business. A packet for the host's own peer never reaches the network: the
/// host's transport hands it to its own World synchronously, so the World has no loopback of its own.
/// No queue: the host's <c>Handle</c> must run in the physics step, before <c>_Process</c>. Then whatever the
/// Presentation puts into <c>HudMailbox</c> is read by the HUD in the same frame, as on a remote client, where the
/// packet is handled in <c>poll</c>, also before <c>_Process</c>.
/// Every send is reliable and ordered. A body is valid only during the call: the sender reuses the buffer for the next
/// peer, so an implementation that sends or loops back later must copy it first.
/// A send returns <c>false</c> when the peer did not get the packet, already logged: the peer may be gone before its
/// <c>peer_disconnected</c> is handled, so it is not an exception.
/// </summary>
public interface IClientsConnection
{
    bool SendState(int peerId, ReadOnlySpan<byte> body);

    bool SendSnapshot(int peerId, ReadOnlySpan<byte> body);

    bool SendEvents(int peerId, ReadOnlySpan<byte> body);

    bool Reject(int peerId, JoinRejectReason reason);

    /// <summary>
    /// After the packets already sent to the peer, so a rejection reaches it. <c>peer_disconnected</c> follows as
    /// for any other disconnection.
    /// </summary>
    void Disconnect(int peerId);

    /// <summary>
    /// The host's own peer, <c>null</c> on a dedicated server.
    /// </summary>
    int? LocalPeerId { get; }
}
