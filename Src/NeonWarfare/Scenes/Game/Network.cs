using System;
using System.Linq;
using Godot;
using Humanizer;
using KludgeBox.Logging;
using Serilog;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// ENet only: raw packets and connection events, no RPC. Knows nothing of the World, <c>Game</c> routes between them.
/// </summary>
// ReSharper disable once Godot.MissingParameterlessConstructor
public partial class Network(Node multiplayerRoot) : Node
{
    private const string SendFailedError = "Sending {0} bytes to peer {1} failed: {2}";
    private const string NotServerError = "Only a server disconnects peers";
    private const string AlreadyGoneLog = "Peer {peerId} not disconnected: it is already gone";

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
        // A fresh multiplayer per Game, so that old handlers and lambdas cannot outlive the session
        _api = new SceneMultiplayer();
        GetTree().SetMultiplayer(_api, multiplayerRoot.GetPath());
        // The tree keeps a custom multiplayer until it is explicitly unset, even after the node is freed
        multiplayerRoot.TreeExiting += OnMultiplayerRootTreeExiting;

        _api.ConnectedToServer += OnMultiplayerConnectedToServer;
        _api.PeerConnected += OnMultiplayerPeerConnected;
        _api.ConnectionFailed += OnMultiplayerConnectionFailed;
        _api.PeerDisconnected += OnMultiplayerPeerDisconnected;
        _api.ServerDisconnected += OnMultiplayerServerDisconnected;
        _api.PeerPacket += OnMultiplayerPeerPacket;
        // A client talks only to the server: a packet from another client must not reach it through the relay
        _api.ServerRelay = false;
    }

    /// <summary>Try to connect to the server</summary>
    /// <returns>
    /// Returns <see cref="Godot.Error.Ok"/> if a client was created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if this <see cref="ENetMultiplayerPeer"/> instance
    /// already has an open connection.<br/>
    /// <see cref="Godot.Error.CantCreate"/> if the client could not be created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if the client already connected.<br/>
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
    /// Try to host server. The server refuses new connections until <see cref="OpenServer"/>, so no client knocks on
    /// a World that does not exist yet.
    /// </summary>
    /// <returns>
    /// Returns <see cref="Godot.Error.Ok"/> if a server was created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if this <see cref="ENetMultiplayerPeer"/> instance
    /// already has an open connection.<br/>
    /// <see cref="Godot.Error.CantCreate"/> if the server could not be created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if the server already hosted.<br/>
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
    /// Reliable and ordered, on <see cref="Consts.TransferChannel.Default"/>.
    /// </summary>
    public void Send(int peerId, ReadOnlySpan<byte> packet)
    {
        Error error = _api.SendBytes(
            packet, peerId, MultiplayerPeer.TransferModeEnum.Reliable, (int) Consts.TransferChannel.Default);
        if (error != Error.Ok)
        {
            throw new InvalidOperationException(SendFailedError.FormatWith(packet.Length, peerId, error));
        }
    }

    /// <summary>
    /// After the packets already queued for the peer, so a rejection reaches it.
    /// </summary>
    public void Disconnect(int peerId)
    {
        if (!IsServer) throw new InvalidOperationException(NotServerError);
        // Already gone between the ticks: its peer_disconnected is reported, and GetPeer of it would be null
        if (!_api.GetPeers().Contains(peerId))
        {
            _log.Warning(AlreadyGoneLog, peerId);
            return;
        }

        // DisconnectPeer resets the peer's queues and would drop a queued rejection; the peer stays in the peer list,
        // so its peer_disconnected is reported once the disconnect is acknowledged
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
