using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Game.Network;

public partial class Network(Node multiplayerRoot) : Node
{

    public static readonly int MaxSyncPacketSize = 1350 * 32;
    
    public MultiplayerApi Api { get; private set; }
    public NetworkStateMachine StateMachine { get; } = new();
    
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
        
        Net.SetGameNetwork(this);
        
        // A fresh multiplayer per Game, so that old handlers and lambdas cannot outlive the session
        GetTree().SetMultiplayer(new SceneMultiplayer(), multiplayerRoot.GetPath());
        // The tree keeps a custom multiplayer until it is explicitly unset, even after the node is freed.
        // Game's own TreeExiting comes after all its children have exited, so spawners and synchronizers
        // still see this multiplayer when they unregister.
        // Not in Shutdown(): Network is Game's last child, so it exits first, before World.
        multiplayerRoot.TreeExiting += ReleaseMultiplayer;

        Api = GetMultiplayer();
        Api.ConnectedToServer += ConnectedToServerEvent;
        Api.PeerConnected += PeerConnectedEvent;
        Api.ConnectionFailed += ConnectionFailedEvent;
        Api.PeerDisconnected += PeerDisconnectedEvent;
        Api.ServerDisconnected += ServerDisconnectedEvent;
        (Api as SceneMultiplayer)?.SetMaxSyncPacketSize(MaxSyncPacketSize);
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
        if (!StateMachine.CanInitialize)
        {
            _log.Error("Can't initialize network in current state: {state}", StateMachine.CurrentState);
            return Error.AlreadyInUse;
        }
        
        _log.Information("Connecting to the server at {host}:{port}", host, port);

        StateMachine.SetState(NetworkStateMachine.State.Connecting);
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(host, port);
        Api.MultiplayerPeer = peer;
		
        if (error != Error.Ok)
        {
            _log.Error("Failed to connect to the server: {error}", error);
        }
        return error; 
    }
    
    /// <summary>
    /// Try to host server.<br/>
    /// If server hosted with <c>refuseNewConnections = true</c>, you must call <c>OpenServer()</c>
    /// after hosting process.
    /// </summary>
    /// <returns>
    /// Returns <see cref="Godot.Error.Ok"/> if a server was created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if this <see cref="ENetMultiplayerPeer"/> instance
    /// already has an open connection.<br/>
    /// <see cref="Godot.Error.CantCreate"/> if the server could not be created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if the server already hosted.<br/>
    /// </returns>
    public Error HostServer(int port, bool refuseNewConnections = false, int maxClients = 32)
    {
        if (!StateMachine.CanInitialize)
        {
            _log.Error("Can't initialize network in current state: {state}", StateMachine.CurrentState);
            return Error.AlreadyInUse;
        }
        
        _log.Information("Starting server on port {port}", port);
        
        StateMachine.SetState(NetworkStateMachine.State.Hosting);
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateServer(port, maxClients);
        peer.RefuseNewConnections = refuseNewConnections;
        Api.MultiplayerPeer = peer;

        if (error == Error.Ok)
        {
            StateMachine.SetState(NetworkStateMachine.State.Hosted);
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
        if (!StateMachine.IsServer)
        {
            _log.Error("Can't open server in current state: {state}", StateMachine.CurrentState);
            return;
        }
        
        Api.MultiplayerPeer.RefuseNewConnections = false;
    }
    
    public override void _Notification(int id)
    {
        if (id == NotificationExitTree) Shutdown();
    }
    
    private void Shutdown()
    {
        Net.RemoveGameNetwork();
        
        if (Api.HasMultiplayerPeer() && Api.GetMultiplayerPeer() is not OfflineMultiplayerPeer)
        {
            _log.Information("Shutting down network...");

            Api.MultiplayerPeer.RefuseNewConnections = true;
            if (StateMachine.IsServer)
            {
                foreach (var peer in Api.GetPeers())
                {
                    Api.MultiplayerPeer.DisconnectPeer(peer);
                }
            }
            Api.MultiplayerPeer.Close();
            Api.MultiplayerPeer = new OfflineMultiplayerPeer();
            StateMachine.SetState(NetworkStateMachine.State.NotInitialized);
            
            _log.Information("Network shutdown successful");
        }
    }

    private void ReleaseMultiplayer()
    {
        SceneTree tree = multiplayerRoot.GetTree();
        NodePath path = multiplayerRoot.GetPath();
        if (tree.GetMultiplayer(path) == Api)
        {
            tree.SetMultiplayer(null, path);
        }
    }

    private void ConnectedToServerEvent()
    {
        StateMachine.SetState(NetworkStateMachine.State.Connected);
        _log.Information("Connected to the server successfully. My peer id: {id}", Api.GetUniqueId());
    }

    private void ConnectionFailedEvent()
    {
        StateMachine.SetState(NetworkStateMachine.State.Disconnected);
        _log.Error("Connection to the server failed");
        
        Shutdown();
    }

    private void ServerDisconnectedEvent()
    {
        StateMachine.SetState(NetworkStateMachine.State.Disconnected);
        _log.Information("Server disconnected");
        
        Shutdown();
    }
    
    private void PeerConnectedEvent(long id)
    {
        _log.Information("Network peer connected: {id}", id);
    }
    
    private void PeerDisconnectedEvent(long id)
    {
        _log.Information("Network peer disconnected: {id}", id);
    }
}