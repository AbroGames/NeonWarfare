using System;
using Godot;

namespace KludgeBox.Net;


public enum NetworkMode
{
    None,
    DedicatedServer,
    ConnectedToRemote,
    Singleplayer,
    Multiplayer
}

/// <summary>
/// Provides sided flags for multiplayer sides.
/// Note that if both flags are presented on the side, then their methods will be executed both directly and via RPC.
/// </summary>
[Flags]
public enum RemoteMode : byte
{
    None = 0b00,
    
    /// <summary>
    /// Represents the multiplayer side that is active in the local application. 
    /// If this flag is presented, then local methods will be executed directly.
    /// </summary>
    Local = 0b01,

    /// <summary>
    /// Represents the multiplayer side that is active in remote application(s). 
    /// If this flag is presented, then remote methods will be executed via RPC.
    /// </summary>
    Remote = 0b10,
    
    Both = 0b11
}

[GlobalClass]
public partial class Network : Node
{
    public static NetworkMode Mode { get; private set; } = NetworkMode.None;
    
    public static Network Instance { get; private set; }
    public static MultiplayerApi Api { get; private set; }
    public static ENetMultiplayerPeer Peer { get; private set; }

    public static bool IsServer { get; private set; } = false;
    public static bool IsClient { get; private set; } = false;

    public static NetworkClient Client => Instance.GetNode("NetworkClient") as NetworkClient;
    public static NetworkServer Server => Instance.GetNode("NetworkServer") as NetworkServer;

    public static event Action<Packet> PacketReceived;
    public static event Action<Packet> ClientPacketReceived;
    public static event Action<Packet> ServerPacketReceived;

    static Network()
    {
    }

    public Network()
    {
        Instance ??= this;
    }

    public override void _Ready()
    {
        Api = GetTree().GetMultiplayer();
        Peer = new ENetMultiplayerPeer();
        //Api.MultiplayerPeer = Peer;

        Api.PeerConnected += id =>
        {
            Log.Info($"[API]({Mode}) Connected new client with id {id}");
            if (id != 1)
            {
                var writer = new DataWriter();
                writer.Write("Hello from server!");
                Server.SendDataToClientReliable((int)id, writer.Data);
            }
        };

        Api.PeerDisconnected += id =>
        {
            Log.Info($"[API]({Mode}) Disconnected client with id {id}");
        };

        Api.ConnectionFailed += () =>
        {
            Log.Warning($"[API]({Mode}) Connection failed");
            Disconnect();
        };

        Api.ConnectedToServer += () =>
        {
            Log.Info($"[API]({Mode}) Connected to server");

            var writer = new DataWriter();
            writer.Write("Hello from client!");
            Client.SendDataReliable(writer.Data);
        };

        Api.ServerDisconnected += () =>
        {
            Log.Info($"[API]({Mode}) Server disconnected");
        };

        
        
        Peer.PeerConnected += id =>
        {
            Log.Debug($"[PEER]({Mode}) Peer #{id} connected");
        };

        Peer.PeerDisconnected += id =>
        {
            Log.Debug($"[PEER]({Mode}) Peer #{id} disconnected");
        };

        PacketReceived += (packet) =>
        {
            var reader = packet.StartReading();
            var msg = reader.ReadString();
            
            Log.Debug($"({Mode}) Packet received: {msg}");
        };
    }
    

    public static void Disconnect()
    {
        if (Server.IsRemote)
        {
            Peer.DisconnectPeer(1);
        }
        else
        {
            Server.Disconnect();
        }
        
        if (Client.IsRemote)
        {
            foreach (var peer in Api.GetPeers())
            {
                Peer.DisconnectPeer(peer);
            }
        }
        else
        {
            Client.Disconnect();
        }
        
        
        IsServer = false;
        IsClient = false;

        Mode = NetworkMode.None;
        Peer.Close();
    }

    internal static bool ReceivePacket(Packet packet)
    {
        PacketReceived?.Invoke(packet);
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        // Must ignore by default and just return packet.Processed = false
        switch (packet.SenderType)
        {
            case PacketSource.Server:
                ClientPacketReceived?.Invoke(packet);
                break;
            case PacketSource.Client:
                ServerPacketReceived?.Invoke(packet);
                break;
        }

        return packet.Processed;
    }
    
    internal static Error RunDedicatedServer(int port, int maxPlayers = DefaultNetworkSettings.DefaultMaxPlayers)
    {
        Error error = Peer.CreateServer(port, maxPlayers);
        if (error is Error.Ok)
        {
            Api.MultiplayerPeer = Peer;
            
            IsServer = true;
            IsClient = false;
            
            Client.ConfigureRemote();
            Server.ConfigureLocal();

            Mode = NetworkMode.DedicatedServer;
        }
        
        Log.Info($"({Mode}) Attempt to run dedicated server finished with status: {error}");
        return error;
    }

    internal static Error ConnectToRemoteServer(string host, int port)
    {
        Error error = Peer.CreateClient(host, port);
        if (error is Error.Ok)
        {
            Api.MultiplayerPeer = Peer;
            
            IsServer = false;
            IsClient = true;
            
            Client.ConfigureLocal();
            Server.ConfigureRemote();

            Mode = NetworkMode.ConnectedToRemote;
        }
        
        Log.Info($"({Mode}) Attempt to connect to remote server finished with status: {error}");
        return error;
    }

    internal static Error RunSingleplayerGame()
    {
        IsServer = true;
        IsClient = true;
        
        Client.ConfigureLocal();
        Server.ConfigureLocal();

        Mode = NetworkMode.Singleplayer;

        return Error.Ok;
    }

    internal static Error RunMultiplayerGame(int port, int maxPlayers = DefaultNetworkSettings.DefaultMaxPlayers)
    {
        Error error = Peer.CreateServer(port, maxPlayers);
        if (error is Error.Ok)
        {
            Api.MultiplayerPeer = Peer;
            
            IsServer = true;
            IsClient = true;
        
            Client.ConfigureLocalAndRemote();
            Server.ConfigureLocal();

            Mode = NetworkMode.Multiplayer;
        }

        Log.Info($"({Mode}) Attempt to run multiplayer game finished with status: {error}");
        return error;
    }
}