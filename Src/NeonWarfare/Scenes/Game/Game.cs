using System;
using Godot;
using GodotBox;
using GodotBox.Godot;
using GodotBox.Godot.Nodes;
using KludgeBox.DI.Requests.ChildInjection;
using NeonWarfare.Scenes.Game.Starters;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Screen.Hud;
using NeonWarfare.Scenes.Screen.ServerHud;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Ports;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// One game session: its scene, its protocol objects, its network, transport and World. The starter brings it up
/// through the calls below; each mode has its own, so a session is built only in the order its mode allows.
/// </summary>
public partial class Game : Node2D
{
    private const string NotInTreeError =
        "Game must be in the tree before Init: WorldPackedScenes fills its scene list in _Ready";
    private const string NoServerNetworkError = "A dedicated server needs AddServerNetwork first";
    private const string NoClientNetworkError = "A remote client needs AddClientNetwork first";
    private const string NoWorldError = "There is no World to show";

    [Child] private NodeContainer WorldContainer { get; set; }
    [Child] private NodeContainer HudContainer { get; set; }
    [Child] private GamePackedScenes GamePackedScenes { get; set; }
    [Child] private WorldPackedScenes WorldPackedScenes { get; set; }

    private GameProtocol _protocol;
    private Network _serverNetwork;
    private ClientTransport _clientTransport;
    private WorldSetup.RemoteClient _clientSetup;
    private World _world;

    public override void _Ready()
    {
        Di.Process(this);
    }

    public void Init(BaseGameStarter gameStarter)
    {
        if (!IsInsideTree()) throw new InvalidOperationException(NotInTreeError);

        _protocol = GameProtocol.Create(WorldPackedScenes);
        gameStarter.Init(this);
    }

    /// <summary>
    /// The network of a host or a dedicated server; its World is added next.
    /// </summary>
    public Network AddServerNetwork()
    {
        _serverNetwork = AddNetwork();
        return _serverNetwork;
    }

    /// <summary>
    /// The network of a remote client. The join is sent once connected; the World is added from the join snapshot,
    /// which <paramref name="snapshotReceived"/> gets.
    /// </summary>
    public Network AddClientNetwork(
        LocalPlayer localPlayer, ILocalPlayerOwner owner, Action<byte[]> snapshotReceived)
    {
        Network network = AddNetwork();
        _clientTransport = new ClientTransport(network, _protocol.Codec, localPlayer, owner, snapshotReceived);
        _clientSetup = new WorldSetup.RemoteClient(localPlayer, owner);
        return network;
    }

    /// <summary>
    /// Single player, or a host over <see cref="AddServerNetwork"/>. The host's own join is sent right away and
    /// applied in the next tick.
    /// </summary>
    public World AddHostWorld(WorldSetup.Host setup, WorldOrigin origin)
    {
        var world = new World();
        ServerTransport remote = _serverNetwork != null ? new ServerTransport(_serverNetwork, world) : null;
        var transport = new HostTransport(world, _protocol.Codec, setup.LocalPlayerOwner, remote);
        BuildWorld(world, setup, origin, transport, transport, remote);
        transport.SendJoinRequest(setup.LocalPlayer);
        return world;
    }

    public World AddDedicatedWorld(WorldSetup.Dedicated setup, WorldOrigin origin)
    {
        if (_serverNetwork == null) throw new InvalidOperationException(NoServerNetworkError);

        var world = new World();
        var transport = new ServerTransport(_serverNetwork, world);
        BuildWorld(world, setup, origin, transport, new NoConnection(), transport);
        return world;
    }

    /// <exception cref="NetMessageFormatException">The snapshot is broken.</exception>
    public World AddClientWorld(byte[] snapshot)
    {
        if (_clientTransport == null) throw new InvalidOperationException(NoClientNetworkError);

        var world = new World();
        BuildWorld(world, _clientSetup, new WorldOrigin.FromSnapshot(snapshot), new NoConnection(), _clientTransport,
            null);
        _clientTransport.Enter(world);
        return world;
    }

    public void ShowHud()
    {
        if (_world == null) throw new InvalidOperationException(NoWorldError);

        Hud hud = GamePackedScenes.Hud.Instantiate<Hud>().InitPreReady(_world, _world);
        hud.SetName("Hud");
        HudContainer.ChangeStoredNode(hud);
    }

    public void ShowServerHud()
    {
        if (_world == null) throw new InvalidOperationException(NoWorldError);

        ServerHud serverHud = GamePackedScenes.ServerHud.Instantiate<ServerHud>().InitPreReady(_world);
        serverHud.SetName("ServerHud");
        HudContainer.ChangeStoredNode(serverHud);
    }

    private Network AddNetwork()
    {
        var network = new Network(this);
        this.AddChildWithName(network, "Network");
        return network;
    }

    /// <param name="serverTransport">Detached from the network when the World fails to be built.</param>
    private void BuildWorld(
        World world, WorldSetup setup, WorldOrigin origin, IClientsConnection clients, IServerConnection server,
        ServerTransport serverTransport)
    {
        var dependencies = new WorldDependencies(
            TimeProvider.System, _protocol.Codec, _protocol.Replicator, FrameProvider.Engine, WorldPackedScenes,
            _protocol.Entities, clients, server);
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
        _world = world;
        _world.SetName("World");
        WorldContainer.ChangeStoredNode(_world);
        HudContainer.ClearStoredNode();
    }
}
