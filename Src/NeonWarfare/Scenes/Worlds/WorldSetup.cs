using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// The configuration of a World, chosen by its starter: the layers and the ports of the process that configuration
/// needs. Known only to the composition root; a service sees its layer and the ports, never the configuration.
/// </summary>
public abstract record WorldSetup
{
    private const WorldLayer ServerLayers =
        WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler | WorldLayer.Server |
        WorldLayer.Query;

    private WorldSetup() { }

    public abstract WorldLayer Layers { get; }

    /// <summary>A client connected to a remote server.</summary>
    public sealed record RemoteClient(LocalPlayer LocalPlayer) : WorldSetup
    {
        public override WorldLayer Layers =>
            WorldLayer.Query | WorldLayer.Client | WorldLayer.Presentation | WorldLayer.ClientReplication;
    }

    /// <summary>Single player and hosting from inside the client: the admin is the host's own player.</summary>
    public sealed record Host(ISaveFiles SaveFiles, LocalPlayer LocalPlayer, IServerOwner Owner) : WorldSetup
    {
        public WorldAdmin Admin => new(LocalPlayer.Uid);

        // No ClientReplication: the host's Simulation writes the very models its Presentation reads
        public override WorldLayer Layers => ServerLayers | WorldLayer.Client | WorldLayer.Presentation;
    }

    /// <summary>A dedicated server, with <c>ServerHud</c> or without.</summary>
    public sealed record Dedicated(ISaveFiles SaveFiles, WorldAdmin Admin, IServerOwner Owner) : WorldSetup
    {
        public override WorldLayer Layers => ServerLayers;
    }
}
