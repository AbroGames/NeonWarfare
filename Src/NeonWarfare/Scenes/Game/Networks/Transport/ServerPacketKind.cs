namespace NeonWarfare.Scenes.Game.Networks.Transport;

/// <summary>
/// The first byte of every packet the server sends. It tells the client what follows.
/// <see cref="JoinRejected"/> never changes its value, because a client of another build reads the rejection of a
/// protocol mismatch.
/// </summary>
public enum ServerPacketKind : byte
{
    Snapshot = 1,
    JoinRejected = 4,
    TickUpdate = 5
}
