using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Worlds;

/// <summary>
/// The configuration of a World: its layers and the ports of the process it needs, see Docs/World.md#layers.
/// </summary>
public abstract record WorldSetup
{
    private const WorldLayer ServerLayers =
        WorldLayer.Simulation | WorldLayer.SimulationFacade | WorldLayer.CommandHandler | WorldLayer.Server |
        WorldLayer.Query;

    private WorldSetup() { }

    public abstract WorldLayer Layers { get; }

    /// <summary>A client connected to a remote server.</summary>
    public sealed record RemoteClient(LocalPlayer LocalPlayer, ILocalPlayerOwner LocalPlayerOwner) : WorldSetup
    {
        public override WorldLayer Layers =>
            WorldLayer.Query | WorldLayer.Client | WorldLayer.Presentation | WorldLayer.ClientReplication;
    }

    /// <summary>Single player and hosting from inside the client. The admin is the host's own player.</summary>
    public sealed record Host(
        ISaveFiles SaveFiles,
        WorldSaveFile SaveFile,
        LocalPlayer LocalPlayer,
        IServerOwner ServerOwner,
        ILocalPlayerOwner LocalPlayerOwner)
        : WorldSetup
    {
        public WorldAdmin Admin => new(LocalPlayer.Uid);

        /// <summary>
        /// There is no <c>ClientReplication</c>, because the host's Simulation writes the very models its Presentation
        /// reads.
        /// </summary>
        public override WorldLayer Layers => ServerLayers | WorldLayer.Client | WorldLayer.Presentation;
    }

    /// <summary>A dedicated server, with <c>ServerHud</c> or without.</summary>
    public sealed record Dedicated(
        ISaveFiles SaveFiles, WorldSaveFile SaveFile, WorldAdmin Admin, IServerOwner ServerOwner) : WorldSetup
    {
        public override WorldLayer Layers => ServerLayers;
    }
}
