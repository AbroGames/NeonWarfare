using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Tick;

namespace NeonWarfare.Scenes.Worlds.Infra.Client.Tick;

/// <summary>
/// The tick counter of a remote client. It stays 0 until the client replication writes it.
/// </summary>
[ClientReplication]
public class ClientTickClock : IWorldClock
{
    public long CurrentTick { get; private set; }
}
