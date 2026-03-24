using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using KludgeBox.Core.Cooldown;

namespace NeonWarfare.Scenes.World.Service.Performance;

public partial class WorldENetPerformance : Node
{

    public record PeerInfo(double Ping, double PacketLoss);

    // Contain delta between last and current calling method UpdateMetrics
    public double SentKb { get; private set; }
    public double ReceivedKb { get; private set; }
    public double SentPackets { get; private set; }
    public double ReceivedPackets { get; private set; }
    public Dictionary<int, PeerInfo> InfoByPeerId => _infoByPeerId;

    private readonly Dictionary<int, PeerInfo> _infoByPeerId = new();
    
    private const double UpdateMetricsInterval = 1.0;
    private AutoCooldown _cooldown;

    public override void _Ready()
    {
        _cooldown = new(UpdateMetricsInterval, true, UpdateMetrics);
    }

    public override void _Process(double delta)
    {
        _cooldown.Update(delta);
    }
    
    public String GetTotalInfoOneLineString()
    {
        StringBuilder sb = new();
        
        sb.Append($"Net (S/L): {SentKb:N0}/{ReceivedKb:N0} kb/s    ");
        sb.Append($"{SentPackets:N0}/{ReceivedPackets:N0} pack/s\n");

        return sb.ToString();
    }
    
    public String GetServerPeerOneLineString()
    {
        if (!_infoByPeerId.ContainsKey(ServerId)) return "";
        
        StringBuilder sb = new();
        
        sb.Append($"Server: ping {_infoByPeerId[ServerId].Ping} ms, packet loss {_infoByPeerId[ServerId].PacketLoss:N2}%\n");

        return sb.ToString();
    }
    
    public String GetPerPeerInfoManyLineString()
    {
        StringBuilder sb = new();

        sb.Append("Peers:\n");
        foreach (KeyValuePair<int, PeerInfo> peerInfo in _infoByPeerId)
        {
            sb.Append($"{peerInfo.Key}: ping {peerInfo.Value.Ping} ms, packet loss {peerInfo.Value.PacketLoss:N2}%\n");
        }

        return sb.ToString();
    }

    private void UpdateMetrics()
    {
        if (Multiplayer.MultiplayerPeer is ENetMultiplayerPeer peer &&
            peer.Host != null &&
            peer.GetConnectionStatus() == MultiplayerPeer.ConnectionStatus.Connected)
        {
            ENetConnection host = peer.Host;

            // PopStatistic returns the value and resets the internal counter.
            // Calling it every 1 second gives us the exact rate per second.
            SentKb = host.PopStatistic(ENetConnection.HostStatistic.SentData) / 1024;
            ReceivedKb = host.PopStatistic(ENetConnection.HostStatistic.ReceivedData) / 1024;
            SentPackets = host.PopStatistic(ENetConnection.HostStatistic.SentPackets);
            ReceivedPackets = host.PopStatistic(ENetConnection.HostStatistic.ReceivedPackets);

            // Per-peer statistics
            _infoByPeerId.Clear();
            foreach (int peerId in Multiplayer.GetPeers())
            {
                ENetPacketPeer clientPeer = peer.GetPeer(peerId);
                if (clientPeer != null)
                {
                    double ping = clientPeer.GetStatistic(ENetPacketPeer.PeerStatistic.RoundTripTime);
                    double packetLoss = clientPeer.GetStatistic(ENetPacketPeer.PeerStatistic.PacketLoss) / ENetPacketPeer.PacketLossScale * 100;
                    _infoByPeerId[peerId] = new PeerInfo(ping, packetLoss);
                }
            }
        }
    }
}