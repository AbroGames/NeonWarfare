using System;
using GodotBox.Godot;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// Builds the World of each configuration over its transports. The World is returned outside the tree; a World that
/// throws while being built is freed.
/// </summary>
public class WorldAssembler(GameProtocol protocol, WorldPackedScenes scenes)
{
    /// <summary>
    /// Single player, or a host over <paramref name="network"/>. The host's own join is sent right away and applied in
    /// the next tick.
    /// </summary>
    /// <param name="network"><c>null</c> in single player.</param>
    public World Host(WorldSetup.Host setup, WorldOrigin origin, INetwork network)
    {
        var world = new World();
        ServerTransport remote = network != null ? new ServerTransport(network, world) : null;
        var transport = new HostTransport(world, protocol.Codec, setup.LocalPlayerOwner, remote);
        Build(world, setup, origin, transport, transport, remote);
        transport.SendJoinRequest(setup.LocalPlayer);
        return world;
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

    /// <param name="serverTransport">Detached from the network when the World fails to be built.</param>
    private void Build(
        World world, WorldSetup setup, WorldOrigin origin, IClientsConnection clients, IServerConnection server,
        ServerTransport serverTransport)
    {
        var dependencies = new WorldDependencies(
            TimeProvider.System, protocol.Codec, protocol.Replicator, FrameProvider.Engine, scenes, protocol.Entities,
            clients, server);
        try
        {
            world.InitPreReady(setup, dependencies, origin);
        }
        catch
        {
            serverTransport?.Dispose();
            // Outside the tree nothing else frees it, nor the entities a snapshot has already spawned in it
            world.Free();
            throw;
        }
    }
}
