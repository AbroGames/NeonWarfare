using System;
using KludgeBox.Logging;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;
using Serilog;

namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The connection of the host, which is the server and a client in one process, see
/// Docs/Networking.md#transport-and-roles.
/// The remote peers, if any, go to <paramref name="remote"/>.
/// </summary>
public sealed class HostTransport(
    World.IServerEndpoint server,
    World.IHostEndpoint client,
    NetMessageCodec codec,
    ProtocolHash protocolHash,
    ILocalPlayerOwner owner,
    ServerTransport remote
    ) : IClientsConnection, IServerConnection
{
    private const int LocalPeer = Consts.Global.ServerId;

    private readonly ILogger _log = LogFactory.GetForStatic<HostTransport>();

    public int? LocalPeerId => LocalPeer;

    /// <summary>
    /// The host's own peer first connects, as a remote one would, so its join passes the same gatekeeper.
    /// </summary>
    public void SendJoinRequest(LocalPlayer localPlayer)
    {
        server.StartHandshake(LocalPeer);
        ((IServerConnection) this).Send(codec.Encode(localPlayer.ToJoinRequest(protocolHash)));
    }

    bool IClientsConnection.SendSnapshot(int peerId, long tick, ReadOnlySpan<byte> records) =>
        Remote(peerId).SendSnapshot(peerId, tick, records);

    /// <exception cref="InvalidOperationException">The host's own peer gets a state section.</exception>
    bool IClientsConnection.SendTickUpdate(int peerId, long tick, ReadOnlySpan<byte> state, ReadOnlySpan<byte> events)
    {
        if (peerId != LocalPeer) return Remote(peerId).SendTickUpdate(peerId, tick, state, events);
        if (!state.IsEmpty)
        {
            throw new InvalidOperationException(
                $"Peer {peerId} is the host's own: its World is the server's and needs no state");
        }

        try
        {
            client.ReceiveOwnEvents(events.ToArray());
        }
        // The events are delivered inside the server's tick, so a failure of the host's own World must not cost the
        // remote peers their events.
        // It still returns true, because false would make the tick disconnect the host's own peer.
        catch (Exception e)
        {
            _log.Error(e, "The host's own World failed to take the events of the tick");
        }
        return true;
    }

    bool IClientsConnection.SendDisconnectReason(int peerId, DisconnectReason reason)
    {
        if (peerId == LocalPeer)
        {
            _log.Error("The server is disconnecting the host's own player: {reason}", reason);
            owner.Disconnected(reason);
            return true;
        }
        return Remote(peerId).SendDisconnectReason(peerId, reason);
    }

    void IClientsConnection.Disconnect(int peerId)
    {
        if (peerId != LocalPeer)
        {
            Remote(peerId).Disconnect(peerId);
            return;
        }

        // It is queued like any other disconnection, so it is processed in the next tick, not inside this one.
        // A warning is enough, because whatever cut the host off has already logged its error.
        _log.Warning("The host's own peer is disconnected: the host has no player");
        server.QueueDisconnection(peerId);
    }

    void IServerConnection.Send(ReadOnlySpan<byte> packet) => server.ReceiveFromClient(LocalPeer, packet.ToArray());

    private ServerTransport Remote(int peerId)
    {
        if (peerId == LocalPeer)
        {
            throw new InvalidOperationException(
                $"Peer {peerId} is the host's own: its World is the server's and needs no state");
        }

        return remote
               ?? throw new InvalidOperationException($"Peer {peerId} is not the host's own, but there is no network");
    }
}
