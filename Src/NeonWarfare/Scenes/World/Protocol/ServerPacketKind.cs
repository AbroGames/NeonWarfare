namespace NeonWarfare.Scenes.World.Protocol;

/// <summary>
/// The first byte of every packet the server sends: it tells the client what follows.
/// </summary>
public enum ServerPacketKind : byte
{
    Events = 1
}
