using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// The state of every peer from its connection to its <c>peer_disconnected</c>, see
/// Docs/Networking.md#join-and-leave. Never replicated, because the models know players only by uid.
/// <see cref="Leaving"/> keeps the uid taken until <c>peer_disconnected</c>, because its <c>Leave</c> is still due.
/// Otherwise a peer rejoining with that uid would lose its online status to the late <c>Leave</c> of the old one.
/// </summary>
[Server]
public class PeerStateTable
{
    public abstract record PeerState;
    public record Connecting(DateTimeOffset Deadline) : PeerState;
    public record Joined(string Uid) : PeerState
    {
        /// <summary>
        /// The encoded events of the tick in the order of publication. <c>EventOutbox</c> fills and drains it.
        /// </summary>
        public List<ReadOnlyMemory<byte>> Events { get; } = [];
    }
    public record Leaving(string Uid) : PeerState;
    public record TurnedAway : PeerState;

    private readonly Dictionary<int, PeerState> _stateByPeerId = new();
    private readonly Dictionary<string, int> _peerIdByUid = new();

    public IEnumerable<int> JoinedPeerIds =>
        _stateByPeerId.Where(pair => pair.Value is Joined).Select(pair => pair.Key);

    public IEnumerable<Joined> AllJoined => _stateByPeerId.Values.OfType<Joined>();

    public void Connect(int peerId, DateTimeOffset deadline)
    {
        if (_stateByPeerId.TryGetValue(peerId, out PeerState state))
        {
            throw new InvalidOperationException($"Peer {peerId} cannot connect: it is already {state}.");
        }

        _stateByPeerId.Add(peerId, new Connecting(deadline));
    }

    public void Join(int peerId, string uid)
    {
        if (_peerIdByUid.TryGetValue(uid, out int takenBy))
        {
            throw new InvalidOperationException($"Uid {uid} cannot join from peer {peerId}: peer {takenBy} has it.");
        }
        if (!IsConnecting(peerId))
        {
            string state = _stateByPeerId.GetValueOrDefault(peerId)?.ToString() ?? "unknown";
            throw new InvalidOperationException($"Peer {peerId} cannot join: it is {state}.");
        }

        _stateByPeerId[peerId] = new Joined(uid);
        _peerIdByUid.Add(uid, peerId);
    }

    /// <returns>Whether the peer has just been cut off. Only then does the caller disconnect it.</returns>
    public bool Disconnect(int peerId)
    {
        _stateByPeerId.TryGetValue(peerId, out PeerState state);
        PeerState next = state switch
        {
            null or Connecting => new TurnedAway(),
            Joined joined => new Leaving(joined.Uid),
            _ => null,
        };
        if (next == null) return false;

        _stateByPeerId[peerId] = next;
        return true;
    }

    /// <summary>
    /// Frees the uid of a leaving peer before its <c>peer_disconnected</c>.
    /// Called once the <c>Leave</c> of the peer has run or will never run.
    /// </summary>
    public void Release(int peerId)
    {
        _stateByPeerId.TryGetValue(peerId, out PeerState state);
        if (state is not Leaving leaving)
        {
            throw new InvalidOperationException(
                $"Peer {peerId} cannot be released: it is {state?.ToString() ?? "unknown"}.");
        }

        _stateByPeerId[peerId] = new TurnedAway();
        _peerIdByUid.Remove(leaving.Uid);
    }

    /// <returns>The last state of the peer, or <c>null</c> for an unknown one.</returns>
    public PeerState Remove(int peerId)
    {
        if (!_stateByPeerId.Remove(peerId, out PeerState state)) return null;

        string uid = state switch
        {
            Joined joined => joined.Uid,
            Leaving leaving => leaving.Uid,
            _ => null,
        };
        if (uid != null) _peerIdByUid.Remove(uid);
        return state;
    }

    public bool TryGetJoined(int peerId, out Joined joined)
    {
        joined = _stateByPeerId.GetValueOrDefault(peerId) as Joined;
        return joined != null;
    }

    public bool TryGetJoinedByUid(string uid, out Joined joined)
    {
        joined = null;
        return _peerIdByUid.TryGetValue(uid, out int peerId) && TryGetJoined(peerId, out joined);
    }

    /// <summary>
    /// It finds a leaving peer too, because the uid is still taken until its <c>peer_disconnected</c>.
    /// </summary>
    public bool TryGetPeerIdByUid(string uid, out int peerId) => _peerIdByUid.TryGetValue(uid, out peerId);

    public bool IsConnecting(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Connecting;

    public bool IsJoined(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Joined;

    public bool IsCut(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Leaving or TurnedAway;

    public List<int> ExpiredConnecting(DateTimeOffset now) => _stateByPeerId
        .Where(pair => pair.Value is Connecting connecting && now >= connecting.Deadline)
        .Select(pair => pair.Key)
        .ToList();
}
