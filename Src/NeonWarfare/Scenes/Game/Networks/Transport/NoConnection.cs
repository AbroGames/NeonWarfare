using System;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The direction a configuration has no use for: a dedicated server sends no commands, and a remote client serves no
/// peers.
/// The layers of that configuration never take it, so a call is a bug.
/// </summary>
public sealed class NoConnection : IClientsConnection, IServerConnection
{
    public int? LocalPeerId => null;

    public bool SendTickUpdate(int peerId, long tick, ReadOnlySpan<byte> state, ReadOnlySpan<byte> events) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public bool SendSnapshot(int peerId, long tick, ReadOnlySpan<byte> records) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public bool SendDisconnectReason(int peerId, DisconnectReason reason) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public void Disconnect(int peerId) =>
        throw new InvalidOperationException("This World has no connection in this direction");

    public void Send(ReadOnlySpan<byte> packet) =>
        throw new InvalidOperationException("This World has no connection in this direction");
}
