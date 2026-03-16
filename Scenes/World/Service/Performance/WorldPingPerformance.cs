using System;
using System.Text;
using Godot;
using KludgeBox.Core.Ping;

namespace NeonWarfare.Scenes.World.Service.Performance;

public partial class WorldPingPerformance : Node
{

    public static class Settings
    {
        public const double PingCooldown = 0.5;
        public const double MaxPingTimeout = 1;

        public const int MaxTimeOfAnalyticalSlidingWindowForPing = 60 * 1000;
        public const int MaxTimeOfAnalyticalSlidingWindowForPacketLoss = 300 * 1000;
        public const int MidTimeOfAnalyticalSlidingWindowForPacketLoss = 30 * 1000;
        public const int ShortTimeOfAnalyticalSlidingWindowForPacketLoss = 5 * 1000;
    }
    
    public long CurrentPingTime => _pingChecker.PingAnalyzer.CurrentPingTime;
    public long MinimumPingTime => _pingChecker.PingAnalyzer.MinimumPingTime;
    public double AveragePingTime => _pingChecker.PingAnalyzer.AveragePingTime;
    public long MaximumPingTime => _pingChecker.PingAnalyzer.MaximumPingTime;
    public double P50PingTime => _pingChecker.PingAnalyzer.P50PingTime;
    public double P90PingTime => _pingChecker.PingAnalyzer.P90PingTime;
    public double P99PingTime => _pingChecker.PingAnalyzer.P99PingTime;
    public double AveragePacketLossInPercentForLongTime => _pingChecker.PingAnalyzer.AveragePacketLossInPercentForLongTime;
    public double AveragePacketLossInPercentForMidTime => _pingChecker.PingAnalyzer.AveragePacketLossInPercentForMidTime;
    public double AveragePacketLossInPercentForShortTime => _pingChecker.PingAnalyzer.AveragePacketLossInPercentForShortTime;

    private PingChecker _pingChecker;

    public override void _Ready()
    {
        PingAnalyzer pingAnalyzer = new PingAnalyzer(
            Settings.MaxTimeOfAnalyticalSlidingWindowForPing,
            Settings.MaxTimeOfAnalyticalSlidingWindowForPacketLoss,
            Settings.MidTimeOfAnalyticalSlidingWindowForPacketLoss,
            Settings.ShortTimeOfAnalyticalSlidingWindowForPacketLoss
        );
        _pingChecker = new PingChecker(SendPingToServer, Settings.PingCooldown, Settings.MaxPingTimeout, pingAnalyzer);
    }

    public void Start()
    {
        _pingChecker.Start();
    }

    public override void _Process(double delta)
    {
        _pingChecker.OnProcess(delta);
    }

    public String GetManyLinesString()
    {
        PingAnalyzer analyzer = _pingChecker.PingAnalyzer;
        StringBuilder sb = new();
        sb.Append($"Ping: {analyzer.CurrentPingTime} ms\n");

        sb.Append($"Ping min/avg/max ({Settings.MaxTimeOfAnalyticalSlidingWindowForPing / 1000}s): ");
        sb.Append($"{analyzer.MinimumPingTime:N1}/{analyzer.AveragePingTime:N1}/{analyzer.MaximumPingTime:N1} ms\n");
        
        sb.Append($"Ping P50/P90/P99 ({Settings.MaxTimeOfAnalyticalSlidingWindowForPing/1000}s): ");
        sb.Append($"{analyzer.P50PingTime:N1}/{analyzer.P90PingTime:N1}/{analyzer.P99PingTime:N1} ms\n");
        
        sb.Append($"Packet loss ({Settings.ShortTimeOfAnalyticalSlidingWindowForPacketLoss / 1000}s/{Settings.MidTimeOfAnalyticalSlidingWindowForPacketLoss / 1000}s/{Settings.MaxTimeOfAnalyticalSlidingWindowForPacketLoss / 1000}s): ");
        sb.Append($"{analyzer.AveragePacketLossInPercentForShortTime:N2}/{analyzer.AveragePacketLossInPercentForMidTime:N2}/{analyzer.AveragePacketLossInPercentForLongTime:N2} %\n");

        return sb.ToString();
    }
    
    private void SendPingToServer(long pingId) => RpcId(ServerId, MethodName.SendPingToServerRpc, pingId);
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void SendPingToServerRpc(long pingId)
    {
        ReturnPingToClient(GetMultiplayer().GetRemoteSenderId(), pingId);
    }

    private void ReturnPingToClient(long peerId, long pingId) => RpcId(peerId, MethodName.ReturnPingToClientRpc, pingId);    
    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void ReturnPingToClientRpc(long pingId)
    {
        _pingChecker.OnReceivedPingPacket(pingId);
    }
}