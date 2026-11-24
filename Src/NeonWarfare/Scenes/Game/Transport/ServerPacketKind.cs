namespace NeonWarfare.Scenes.Game.Transport;

/// <summary>
/// The first byte of every packet the server sends: it tells the client what follows.
/// <see cref="JoinRejected"/> never changes its value: a client of another build reads the rejection of a protocol
/// mismatch.
/// </summary>
public enum ServerPacketKind : byte
{
    Snapshot = 1,
    State = 2,
    Events = 3,
    JoinRejected = 4
}
