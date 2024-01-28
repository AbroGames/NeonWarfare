using System;
using Godot;
using static Godot.MultiplayerPeer;

namespace KludgeBox.Net;

[GlobalClass]
public sealed partial class NetworkClient : Node
{
    /// <summary>
    ///     Indicates if client node is presented on the different dist and must use RPC instead of direct method calls. <br />
    ///     Will be true if game runs in headless mode (as server). <br />
    ///     Will be true if local multiplayer server is started. <br />
    ///     Will be false if server runs in single-player mode. <br />
    ///     Will be false if connected to remote server. <br />
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


    internal void Disconnect()
    {
        if (IsRemote) throw new InvalidOperationException("Can't execute Disconnect() on Client in remote mode");
        IsActive = false;
        IsRemote = false;
        IsLocal = false;
    }

    public void SendDataReliable(byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkClient must be active to send data to server");
        if (!KludgeBox.Net.Network.Server.IsActive) throw new InvalidOperationException("NetworkServer must be active to send data to server");
        
        if (KludgeBox.Net.Network.Server.IsLocal) KludgeBox.Net.Network.Server.ReceiveDataReliable(data);
        if (KludgeBox.Net.Network.Server.IsRemote) KludgeBox.Net.Network.Server.ReceiveDataByRpcReliable(data);
    }

    public void SendDataUnreliable(byte[] data)
    {
        if (!IsLocal || !IsActive) throw new InvalidOperationException("Local NetworkClient must be active to send data to server");
        if (!KludgeBox.Net.Network.Server.IsActive) throw new InvalidOperationException("NetworkServer must be active to send data to server");
        
        if (KludgeBox.Net.Network.Server.IsLocal) KludgeBox.Net.Network.Server.ReceiveDataUnreliable(data);
        if (KludgeBox.Net.Network.Server.IsRemote) KludgeBox.Net.Network.Server.ReceiveDataByRpcUnreliable(data);
    }


    [Rpc(TransferMode = TransferModeEnum.Reliable, CallLocal = false)]
    internal void ReceiveDataReliable(byte[] data)
    {
        var sender = DefaultNetworkSettings.ServerPeerId;
        var packet = new Packet(sender, TransferModeEnum.Reliable, data);
        KludgeBox.Net.Network.ReceivePacket(packet);
    }

    [Rpc(TransferMode = TransferModeEnum.Unreliable, CallLocal = false)]
    public void ReceiveDataUnreliable(byte[] data)
    {
        var sender = DefaultNetworkSettings.ServerPeerId;
        var packet = new Packet(sender, TransferModeEnum.Unreliable, data);
        KludgeBox.Net.Network.ReceivePacket(packet);
    }

    internal void ReceiveDataByRpcReliable(byte[] data)
    {
        Rpc(nameof(ReceiveDataReliable), data);
    }

    internal void ReceiveDataByRpcUnreliable(byte[] data)
    {
        Rpc(nameof(ReceiveDataUnreliable), data);
    }

    internal void ReceiveDataByRpcIdReliable(int peerId, byte[] data)
    {
        RpcId(peerId, nameof(ReceiveDataReliable), data);
    }

    internal void ReceiveDataByRpcIdUnreliable(int peerId, byte[] data)
    {
        RpcId(peerId, nameof(ReceiveDataUnreliable), data);
    }

    internal void ConfigureRemote()
    {
        IsActive = true;
        IsRemote = true;
        IsLocal = false;
    }

    internal void ConfigureLocal()
    {
        IsActive = true;
        IsRemote = false;
        IsLocal = true;
    }

    internal void ConfigureLocalAndRemote()
    {
        IsActive = true;
        IsRemote = true;
        IsLocal = true;
    }
}