using System;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Ports;

/// <summary>
/// The connection from the server World to its clients. The World hands over packet bodies only.
/// The host's own peer is served synchronously, inside the call, see Docs/Networking.md#transport-and-roles.
/// <list type="bullet">
/// <item>There is no queue for the host's own peer. Its event handlers must run in the physics step, before
/// <c>_Process</c>, so the HUD reads <c>HudMailbox</c> in the same frame, as on a remote client.</item>
/// <item>Every send is reliable and ordered.</item>
/// <item>A body is valid only during the call, because the World reuses the buffer for the next peer.
/// An implementation that sends later must copy it.</item>
/// <item>A send returns <c>false</c> when the peer did not get the packet, and the failure is already logged.
/// Not an exception, because a peer may be gone before its <c>peer_disconnected</c> is handled.</item>
/// </list>
/// </summary>
public interface IClientsConnection
{
    bool SendState(int peerId, ReadOnlySpan<byte> body);

    bool SendSnapshot(int peerId, ReadOnlySpan<byte> body);

    bool SendEvents(int peerId, ReadOnlySpan<byte> body);

    bool Reject(int peerId, JoinRejectReason reason);

    /// <summary>
    /// Disconnects the peer after the packets already sent to it, so a rejection still reaches it.
    /// <c>peer_disconnected</c> follows, as for any other disconnection.
    /// </summary>
    void Disconnect(int peerId);

    /// <summary>The host's own peer, <c>null</c> on a dedicated server.</summary>
    int? LocalPeerId { get; }
}
