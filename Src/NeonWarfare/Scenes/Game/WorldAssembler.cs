using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.Game.Networks;
using NeonWarfare.Scenes.Game.Networks.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// Builds the World of each configuration over its transports.
/// The World is returned outside the tree. A World that throws while being built is freed.
/// </summary>
public class WorldAssembler(GameProtocol protocol, WorldPackedScenes scenes)
{
    /// <summary>
    /// Builds the World of a host without a network. Its own join is sent right away and applied in the next tick.
    /// </summary>
    public World Singleplayer(WorldSetup.Host setup, WorldOrigin origin) =>
        BuildHost(new World(), setup, origin, null);

    /// <summary>
    /// Builds the World of a host over <paramref name="network"/>. Its own join is sent right away and applied in the
    /// next tick.
    /// </summary>
    public World Host(WorldSetup.Host setup, WorldOrigin origin, INetwork network)
    {
        var world = new World();
        return BuildHost(world, setup, origin, new ServerTransport(network, world));
    }

    public World Dedicated(WorldSetup.Dedicated setup, WorldOrigin origin, INetwork network)
    {
        var world = new World();
        var transport = new ServerTransport(network, world);
        Build(world, setup, origin, transport, new NoConnection(), transport);
        return world;
    }

    /// <exception cref="NetMessageFormatException">The snapshot is broken.</exception>
    public World RemoteClient(WorldSetup.RemoteClient setup, ReadOnlyMemory<byte> snapshot, ClientTransport transport)
    {
        var world = new World();
        Build(world, setup, new WorldOrigin.FromSnapshot(snapshot), new NoConnection(), transport, null);
        transport.Enter(world);
        return world;
    }

    private World BuildHost(World world, WorldSetup.Host setup, WorldOrigin origin, ServerTransport remote)
    {
        var transport = new HostTransport(world, world, protocol.Codec, protocol.Hash, setup.LocalPlayerOwner, remote);
        Build(world, setup, origin, transport, transport, remote);
        transport.SendJoinRequest(setup.LocalPlayer);
        return world;
    }

    // serverTransport is detached from the network when the World fails to be built.
    private void Build(
        World world, WorldSetup setup, WorldOrigin origin, IClientsConnection clients, IServerConnection server,
        ServerTransport serverTransport)
    {
        var dependencies = new WorldDependencies(
            TimeProvider.System, protocol.Codec, protocol.Hash, protocol.Replicator, FrameProvider.Engine, scenes,
            protocol.Entities, clients, server);
        try
        {
            world.InitPreReady(setup, dependencies, origin);
        }
        catch
        {
            serverTransport?.Dispose();
            // Outside the tree nothing else frees the World or the entities a snapshot has already spawned in it.
            world.Free();
            throw;
        }
    }
}
