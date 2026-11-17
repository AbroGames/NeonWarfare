namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// The first byte of every packet the server sends: it tells the client what follows.
/// </summary>
public enum ServerPacketKind : byte
{
    Snapshot = 1,
    State = 2,
    Events = 3,
    JoinRejected = 4
}
