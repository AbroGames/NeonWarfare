using System;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The direction a configuration has no use for: a dedicated server sends no commands, a remote client serves no
/// peers. Its layers never take it, so a call is a bug.
/// </summary>
public sealed class NoConnection : IClientsConnection, IServerConnection
{
    private const string NoConnectionError = "This World has no connection in this direction";

    public int? LocalPeerId => null;

    public void SendState(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException(NoConnectionError);

    public void SendSnapshot(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException(NoConnectionError);

    public void SendEvents(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException(NoConnectionError);

    public void Reject(int peerId, JoinRejectReason reason) => throw new InvalidOperationException(NoConnectionError);

    public void Disconnect(int peerId) => throw new InvalidOperationException(NoConnectionError);

    public void Send(ReadOnlySpan<byte> packet) => throw new InvalidOperationException(NoConnectionError);
}
