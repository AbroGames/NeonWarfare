namespace NeonWarfare.Scenes.World.Infra.Protocol;

/// <summary>
/// The first byte of every packet the server sends: it tells the client what follows.
/// </summary>
public enum ServerPacketKind : byte
{
    State = 1,
    Events = 2,
    JoinRejected = 3
}
