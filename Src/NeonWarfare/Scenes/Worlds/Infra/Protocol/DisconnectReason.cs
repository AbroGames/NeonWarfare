namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// Why the server disconnects a peer, as a code, so the client shows the reason in its own language.
/// A value is never changed or reused, because a client of another build reads it too.
/// </summary>
public enum DisconnectReason : byte
{
    ProtocolMismatch = 1,
    InvalidUid = 2,
    InvalidNick = 3,
    InvalidColor = 4,
    UidInUse = 5,
    InternalError = 6,
    Displaced = 7,
    HandshakeTimeout = 8,
    MissedPacket = 9
}
