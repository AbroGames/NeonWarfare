using System;
using System.Linq;
using Godot;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scenes.Game.Networks;

/// <summary>
/// ENet only: raw packets and connection events, see Docs/Networking.md#transport-and-roles.
/// It knows nothing of the World. The transports route between them.
/// </summary>
// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class Network(Node multiplayerRoot) : Node, INetwork
{
    public event Action<int> PeerConnected;
    public event Action<int> PeerDisconnected;
    public event Action<int, byte[]> PacketReceived;
    public event Action ConnectedToServer;
    public event Action ConnectionFailed;
    public event Action ServerDisconnected;

    public bool IsServer { get; private set; }

    private SceneMultiplayer _api;
    private bool _isInitialized;

    private readonly ILogger _log = LogFactory.GetForStatic<Network>();

    public override void _Ready()
    {
        // Every Game gets a fresh multiplayer, so no old handler can outlive the session.
        _api = new SceneMultiplayer();
        GetTree().SetMultiplayer(_api, multiplayerRoot.GetPath());
        // The tree keeps a custom multiplayer until it is unset, even after the node is freed.
        multiplayerRoot.TreeExiting += OnMultiplayerRootTreeExiting;

        _api.ConnectedToServer += OnMultiplayerConnectedToServer;
        _api.PeerConnected += OnMultiplayerPeerConnected;
        _api.ConnectionFailed += OnMultiplayerConnectionFailed;
        _api.PeerDisconnected += OnMultiplayerPeerDisconnected;
        _api.ServerDisconnected += OnMultiplayerServerDisconnected;
        _api.PeerPacket += OnMultiplayerPeerPacket;
        // A client talks only to the server, so no packet of another client may reach it through the relay.
        _api.ServerRelay = false;
    }

    /// <returns>
    /// <see cref="Godot.Error.Ok"/> when the client was created.
    /// <see cref="Godot.Error.AlreadyInUse"/> on a second call of either method, even after a failed one.
    /// Otherwise the error of ENet, such as <see cref="Godot.Error.CantCreate"/>.
    /// </returns>
    public Error ConnectToServer(string host, int port)
    {
        if (_isInitialized)
        {
            _log.Error("Can't initialize network: it is already initialized");
            return Error.AlreadyInUse;
        }

        _log.Information("Connecting to the server at {host}:{port}", host, port);

        _isInitialized = true;
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(host, port);
        _api.MultiplayerPeer = peer;

        if (error != Error.Ok)
        {
            _log.Error("Failed to connect to the server: {error}", error);
        }
        return error;
    }

    /// <summary>
    /// Hosts a server that refuses new connections until <see cref="OpenServer"/>, so no client knocks on a World that
    /// does not exist yet.
    /// </summary>
    /// <returns>
    /// <see cref="Godot.Error.Ok"/> when the server was created.
    /// <see cref="Godot.Error.AlreadyInUse"/> on a second call of either method, even after a failed one.
    /// Otherwise the error of ENet, such as <see cref="Godot.Error.CantCreate"/>.
    /// </returns>
    public Error HostServer(int port, int maxClients = 32)
    {
        if (_isInitialized)
        {
            _log.Error("Can't initialize network: it is already initialized");
            return Error.AlreadyInUse;
        }

        _log.Information("Starting server on port {port}", port);

        _isInitialized = true;
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);
        peer.RefuseNewConnections = true;
        _api.MultiplayerPeer = peer;

        if (error == Error.Ok)
        {
            IsServer = true;
            _log.Information("Started server successfully");
        }
        else
        {
            _log.Error("Failed to start server: {error}", error);
        }

        return error;
    }

    public void OpenServer()
    {
        if (!IsServer)
        {
            _log.Error("Can't open server: the server is not hosted");
            return;
        }

        _api.MultiplayerPeer.RefuseNewConnections = false;
        _log.Information("Server opened");
    }

    /// <summary>
    /// Sends the packet reliably and in order on <see cref="Consts.TransferChannel.Default"/>.
    /// A failure is not an exception, because a peer may be gone from ENet before its <c>peer_disconnected</c> is
    /// handled.
    /// </summary>
    /// <returns><c>false</c> when the packet is not sent. The failure is already logged.</returns>
    public bool Send(int peerId, ReadOnlySpan<byte> packet)
    {
        Error error = _api.SendBytes(
            packet, peerId, MultiplayerPeer.TransferModeEnum.Reliable, (int) Consts.TransferChannel.Default);
        if (error == Error.Ok) return true;

        _log.Error("Sending {length} bytes to peer {peerId} failed: {error}", packet.Length, peerId, error);
        return false;
    }

    /// <summary>
    /// Disconnects the peer after the packets already queued for it, so a rejection still reaches it.
    /// </summary>
    public void Disconnect(int peerId)
    {
        if (!IsServer) throw new InvalidOperationException("Only a server disconnects peers");
        // A peer gone between the ticks has its peer_disconnected reported already, and GetPeer would return null.
        if (!_api.GetPeers().Contains(peerId))
        {
            _log.Warning("Peer {peerId} not disconnected: it is already gone", peerId);
            return;
        }

        // DisconnectPeer would reset the peer's queues and drop a queued rejection.
        // With PeerDisconnectLater the peer stays in the list, and its peer_disconnected comes once the disconnect is
        // acknowledged.
        ((ENetMultiplayerPeer) _api.MultiplayerPeer).GetPeer(peerId).PeerDisconnectLater();
    }

    public override void _Notification(int id)
    {
        if (id == NotificationExitTree) Shutdown();
    }

    private void Shutdown()
    {
        if (_api.HasMultiplayerPeer() && _api.GetMultiplayerPeer() is not OfflineMultiplayerPeer)
        {
            _log.Information("Shutting down network...");

            _api.MultiplayerPeer.RefuseNewConnections = true;
            if (IsServer)
            {
                foreach (var peer in _api.GetPeers())
                {
                    _api.MultiplayerPeer.DisconnectPeer(peer);
                }
            }
            _api.MultiplayerPeer.Close();
            _api.MultiplayerPeer = new OfflineMultiplayerPeer();
            IsServer = false;
            _isInitialized = false;

            _log.Information("Network shutdown successful");
        }
    }

    private void OnMultiplayerRootTreeExiting()
    {
        SceneTree tree = multiplayerRoot.GetTree();
        NodePath path = multiplayerRoot.GetPath();
        if (tree.GetMultiplayer(path) == _api)
        {
            tree.SetMultiplayer(null, path);
        }
    }

    private void OnMultiplayerConnectedToServer()
    {
        _log.Information("Connected to the server successfully. My peer id: {id}", _api.GetUniqueId());
        ConnectedToServer?.Invoke();
    }

    private void OnMultiplayerConnectionFailed()
    {
        _log.Error("Connection to the server failed");

        Shutdown();
        ConnectionFailed?.Invoke();
    }

    private void OnMultiplayerServerDisconnected()
    {
        _log.Information("Server disconnected");

        Shutdown();
        ServerDisconnected?.Invoke();
    }

    private void OnMultiplayerPeerConnected(long id)
    {
        _log.Information("Network peer connected: {id}", id);
        if (IsServer) PeerConnected?.Invoke((int) id);
    }

    private void OnMultiplayerPeerDisconnected(long id)
    {
        _log.Information("Network peer disconnected: {id}", id);
        if (IsServer) PeerDisconnected?.Invoke((int) id);
    }

    private void OnMultiplayerPeerPacket(long id, byte[] packet)
    {
        PacketReceived?.Invoke((int) id, packet);
    } 
}
