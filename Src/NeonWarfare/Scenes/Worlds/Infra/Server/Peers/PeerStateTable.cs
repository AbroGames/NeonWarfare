using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Peers;

/// <summary>
/// The state of every peer the server knows of, from its connection to its <c>peer_disconnected</c>, with the uid of
/// each joined player. Not replicated: the model knows only the uid, and the server layer converts it to a peer.
/// Only a <see cref="Joined"/> peer receives packets and has its commands handled. A peer the server has disconnected
/// stays until its <c>peer_disconnected</c>, since it keeps sending until then. <see cref="Leaving"/> keeps the uid
/// taken that long: its <c>Leave</c> is still due, and a peer rejoining with that uid would otherwise lose its
/// "online" to the late <c>Leave</c> of the old one.
/// </summary>
[Server]
public class PeerStateTable
{
    private const string KnownPeerError = "Peer {0} cannot connect: it is already {1}.";
    private const string UidTakenError = "Uid {0} cannot join from peer {1}: peer {2} has it.";
    private const string NotJoinableError = "Peer {0} cannot join: it is {1}.";
    private const string NotLeavingError = "Peer {0} cannot be released: it is {1}.";

    public abstract record PeerState;
    public record Connecting(DateTimeOffset Deadline) : PeerState;
    public record Joined(string Uid) : PeerState
    {
        // Filled by EventOutbox in the order of publication, emptied by sending
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
            throw new InvalidOperationException(KnownPeerError.FormatWith(peerId, state));
        }

        _stateByPeerId.Add(peerId, new Connecting(deadline));
    }

    public void Join(int peerId, string uid)
    {
        if (_peerIdByUid.TryGetValue(uid, out int takenBy))
        {
            throw new InvalidOperationException(UidTakenError.FormatWith(uid, peerId, takenBy));
        }
        if (!IsConnecting(peerId))
        {
            string state = _stateByPeerId.GetValueOrDefault(peerId)?.ToString() ?? "unknown";
            throw new InvalidOperationException(NotJoinableError.FormatWith(peerId, state));
        }

        _stateByPeerId[peerId] = new Joined(uid);
        _peerIdByUid.Add(uid, peerId);
    }

    /// <returns>Whether the peer has just been cut off: the transport disconnects it once.</returns>
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

    // Called once the Leave of the peer is done or will never be, before its peer_disconnected
    public void Release(int peerId)
    {
        _stateByPeerId.TryGetValue(peerId, out PeerState state);
        if (state is not Leaving leaving)
        {
            throw new InvalidOperationException(NotLeavingError.FormatWith(peerId, state?.ToString() ?? "unknown"));
        }

        _stateByPeerId[peerId] = new TurnedAway();
        _peerIdByUid.Remove(leaving.Uid);
    }

    /// <returns>The last state of the peer, null for an unknown one.</returns>
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

    // A leaving peer too: until its peer_disconnected the uid is still its
    public bool TryGetPeerIdByUid(string uid, out int peerId) => _peerIdByUid.TryGetValue(uid, out peerId);

    public bool IsConnecting(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Connecting;

    public bool IsJoined(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Joined;

    public bool IsCut(int peerId) => _stateByPeerId.GetValueOrDefault(peerId) is Leaving or TurnedAway;

    public List<int> ExpiredConnecting(DateTimeOffset now) => _stateByPeerId
        .Where(pair => pair.Value is Connecting connecting && now >= connecting.Deadline)
        .Select(pair => pair.Key)
        .ToList();
}
