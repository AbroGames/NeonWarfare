using Godot;
using KludgeBox.DI.Requests.LoggerInjection;
using Serilog;

namespace NeonWarfare.Scenes.Game.Network;

public partial class Network : Node
{

    public static readonly int MaxSyncPacketSize = 1350 * 100;
    
    public MultiplayerApi Api { get; private set; }
    public NetworkStateMachine StateMachine { get; } = new();
    
    [Logger] private ILogger _log;

    public override void _Ready()
    {
        Di.Process(this);
        
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
    /// <see cref="Godot.Error.AlreadyInUse"/> if this <see cref="ENetMultiplayerPeer"/> instance already has an open connection.<br/>
    /// <see cref="Godot.Error.CantCreate"/> if the client could not be created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if the client already connected.<br/>
    /// </returns>
    public Error ConnectToServer(string host, int port)
    {
        if (!StateMachine.CanInitialize)
        {
            _log.Error($"Can't initialize network in current state: {StateMachine.CurrentState}");
            return Error.AlreadyInUse;
        }
        
        _log.Information($"Connecting to the server at {host}:{port}");

        StateMachine.SetState(NetworkStateMachine.State.Connecting);
        var peer = new ENetMultiplayerPeer();
        var error = peer.CreateClient(host, port);
        Api.MultiplayerPeer = peer;
		
        if (error != Error.Ok)
        {
            _log.Error($"Failed to connect to the server: {error}");
        }
        return error; 
    }
    
    /// <summary>
    /// Try to host server.<br/>
    /// If server hosted with <c>refuseNewConnections = true</c>, you must call <c>OpenServer()</c> after hosting process.
    /// </summary>
    /// <returns>
    /// Returns <see cref="Godot.Error.Ok"/> if a server was created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if this <see cref="ENetMultiplayerPeer"/> instance already has an open connection.<br/>
    /// <see cref="Godot.Error.CantCreate"/> if the server could not be created.<br/>
    /// <see cref="Godot.Error.AlreadyInUse"/> if the server already hosted.<br/>
    /// </returns>
    public Error HostServer(int port, bool refuseNewConnections = false, int maxClients = 32)
    {
        if (!StateMachine.CanInitialize)
        {
            _log.Error($"Can't initialize network in current state: {StateMachine.CurrentState}");
            return Error.AlreadyInUse;
        }
        
        _log.Information($"Starting server on port {port}");
        
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
            _log.Error($"Failed to start server: {error}");
        }
        
        return error;
    }

    public void OpenServer()
    {
        if (!StateMachine.IsServer)
        {
            _log.Error($"Can't open server in current state: {StateMachine.CurrentState}");
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
        if (Api.HasMultiplayerPeer() && Api.GetMultiplayerPeer() is not OfflineMultiplayerPeer)
        {
            _log.Information("Shutting down network...");

            Api.MultiplayerPeer.RefuseNewConnections = true;
            foreach (var peer in Api.GetPeers())
            {
                Api.MultiplayerPeer.DisconnectPeer(peer);
            }
            Api.MultiplayerPeer.Close();
            Api.MultiplayerPeer = new OfflineMultiplayerPeer();
            StateMachine.SetState(NetworkStateMachine.State.NotInitialized);
            
            _log.Information("Network shutdown successful");
        }
    }

    private void ConnectedToServerEvent()
    {
        StateMachine.SetState(NetworkStateMachine.State.Connected);
        _log.Information($"Connected to the server successfully. My peer id: {Api.GetUniqueId()}");
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
        _log.Debug($"Network peer connected: {id}");
    }
    
    private void PeerDisconnectedEvent(long id)
    {
        _log.Debug($"Network peer disconnected: {id}");
    }
}