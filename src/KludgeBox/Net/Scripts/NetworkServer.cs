using System;
using Godot;
using static Godot.MultiplayerApi;
using static Godot.MultiplayerPeer;

namespace KludgeBox.Net;

[GlobalClass]
public sealed partial class NetworkServer : Node
{
    /// <summary>
    ///     Indicates if client node is presented on the different dist and must use RPC instead of direct method calls. <br />
    ///     Will be false if game runs in headless mode (as server). <br />
    ///     Will be false if local multiplayer server is started. <br />
    ///     Will be false if server runs in single-player mode. <br />
    ///     Will be true if connected to remote server. <br />
    /// </summary>
    public RemoteMode Remote { get; private set; }

    /// <summary>
    ///     Indicates if network client is connected to server.
    /// </summary>
    public bool IsActive { get; private set; }

    public bool IsRemote
    {
        get => Remote.HasFlag(RemoteMode.Remote);
        set => Remote = value ? Remote | RemoteMode.Remote : Remote & ~RemoteMode.Remote;
    }

    public bool IsLocal
    {
        get => Remote.HasFlag(RemoteMode.Local);
        set => Remote = value ? Remote | RemoteMode.Local : Remote & ~RemoteMode.Local;
    }

    public void SendDataToClientsUnreliable(byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkServer must be active to send data");
        if (!KludgeBox.Net.Network.Client.IsActive) throw new InvalidOperationException("NetworkClient must be active to send data");
        
        if (KludgeBox.Net.Network.Client.IsLocal) KludgeBox.Net.Network.Client.ReceiveDataUnreliable(data);
        if (KludgeBox.Net.Network.Client.IsRemote) KludgeBox.Net.Network.Client.ReceiveDataByRpcUnreliable(data);
    }

    public void SendDataToClientsReliable(byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkServer must be active to send data");
        if (!KludgeBox.Net.Network.Client.IsActive) throw new InvalidOperationException("NetworkClient must be active to send data");
        
        if (KludgeBox.Net.Network.Client.IsLocal) KludgeBox.Net.Network.Client.ReceiveDataReliable(data);
        if (KludgeBox.Net.Network.Client.IsRemote) KludgeBox.Net.Network.Client.ReceiveDataByRpcReliable(data);
    }

    public void SendDataToClientUnreliable(int peerId, byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkServer must be active to send data");
        if (!KludgeBox.Net.Network.Client.IsActive) throw new InvalidOperationException("NetworkClient must be active to send data");
        
        if (KludgeBox.Net.Network.Client.IsRemote && peerId is not (0 or 1))
            KludgeBox.Net.Network.Client.ReceiveDataByRpcIdUnreliable(peerId, data);

        if (KludgeBox.Net.Network.Client.IsLocal && peerId is 0) KludgeBox.Net.Network.Client.ReceiveDataUnreliable(data);
    }

    public void SendDataToClientReliable(int peerId, byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkServer must be active to send data");
        if (!KludgeBox.Net.Network.Client.IsActive) throw new InvalidOperationException("NetworkClient must be active to send data");
        
        if (KludgeBox.Net.Network.Client.IsRemote && peerId is not (0 or 1)) KludgeBox.Net.Network.Client.ReceiveDataByRpcIdReliable(peerId, data);

        if (KludgeBox.Net.Network.Client.IsLocal && peerId is 0) KludgeBox.Net.Network.Client.ReceiveDataReliable(data);
    }


    [Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    internal void ReceiveDataUnreliable(byte[] data)
    {
        var sender = KludgeBox.Net.Network.Api.GetRemoteSenderId();
        var packet = new Packet(sender, TransferModeEnum.Unreliable, data);
        KludgeBox.Net.Network.ReceivePacket(packet);
    }

    [Rpc(RpcMode.AnyPeer, TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    internal void ReceiveDataReliable(byte[] data)
    {
        var sender = KludgeBox.Net.Network.Api.GetRemoteSenderId();
        var packet = new Packet(sender, TransferModeEnum.Reliable, data);
        KludgeBox.Net.Network.ReceivePacket(packet);
    }


    public void Disconnect()
    {
        if (IsRemote) throw new InvalidOperationException("Can't execute Disconnect() on Server in remote mode");

        foreach (var peer in KludgeBox.Net.Network.Api.GetPeers()) Disconnect(peer);

        IsActive = false;
        IsRemote = false;
        IsLocal = false;
        KludgeBox.Net.Network.Peer.Close();
    }

    public void Disconnect(int peerId)
    {
        KludgeBox.Net.Network.Peer.DisconnectPeer(peerId);
    }

    internal void ConfigureLocal()
    {
        IsActive = true;
        IsRemote = false;
        IsLocal = true;
    }

    internal void ConfigureRemote()
    {
        IsActive = true;
        IsRemote = true;
        IsLocal = true;
    }


    internal void ReceiveDataByRpcReliable(byte[] data)
    {
        Rpc(nameof(ReceiveDataReliable), data);
    }

    internal void ReceiveDataByRpcUnreliable(byte[] data)
    {
        Rpc(nameof(ReceiveDataUnreliable), data);
    }
}