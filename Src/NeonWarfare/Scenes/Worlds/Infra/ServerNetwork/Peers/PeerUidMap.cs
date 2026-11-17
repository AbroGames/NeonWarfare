using System;
using System.Collections.Generic;
using Humanizer;
using NeonWarfare.Scenes.Worlds.Infra.Composition;

namespace NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Peers;

/// <summary>
/// Which peer each joined player is on. Not replicated: the model knows only the uid, and the server network
/// layer converts it to a peer.
/// </summary>
[ServerNetwork]
public class PeerUidMap
{
    private const string UidBoundError = "Uid {0} is already bound to peer {1}.";
    private const string PeerBoundError = "Peer {0} is already bound to uid {1}.";
    private const string PeerNotBoundError = "Peer {0} is not bound to any uid.";

    private readonly Dictionary<string, int> _peerIdByUid = new();
    private readonly Dictionary<int, string> _uidByPeerId = new();

    // Displacing a player from another peer is an explicit Unbind first, never a silent overwrite
    public void Bind(string uid, int peerId)
    {
        if (_peerIdByUid.TryGetValue(uid, out int boundPeerId))
        {
            throw new InvalidOperationException(UidBoundError.FormatWith(uid, boundPeerId));
        }
        if (_uidByPeerId.TryGetValue(peerId, out string boundUid))
        {
            throw new InvalidOperationException(PeerBoundError.FormatWith(peerId, boundUid));
        }

        _peerIdByUid.Add(uid, peerId);
        _uidByPeerId.Add(peerId, uid);
    }

    public void Unbind(int peerId)
    {
        if (!_uidByPeerId.Remove(peerId, out string uid))
        {
            throw new InvalidOperationException(PeerNotBoundError.FormatWith(peerId));
        }

        _peerIdByUid.Remove(uid);
    }

    public bool TryGetPeerId(string uid, out int peerId) => _peerIdByUid.TryGetValue(uid, out peerId);

    public bool TryGetUid(int peerId, out string uid) => _uidByPeerId.TryGetValue(peerId, out uid);
}
