using System;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The direction a configuration has no use for: a dedicated server sends no commands, a remote client serves no
/// peers. Its layers never take it, so a call is a bug.
/// </summary>
public sealed class NoConnection : IClientsConnection, IServerConnection
{
    public int? LocalPeerId => null;

    public bool SendState(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public bool SendSnapshot(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public bool SendEvents(int peerId, ReadOnlySpan<byte> body) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public bool Reject(int peerId, JoinRejectReason reason) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public void Disconnect(int peerId) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public void Send(ReadOnlySpan<byte> packet) =>
        throw new InvalidOperationException("This World has no connection in this direction");
}
