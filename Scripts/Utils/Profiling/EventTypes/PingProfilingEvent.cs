using System.IO;
using NeonWarfare.Scenes.Game.ClientGame.Ping;

namespace NeonWarfare.Scripts.Utils.Profiling;

public class PingProfilingEvent : ProfilingEvent
{
    public long CurrentPingTime { get; set; }
    public long MinimumPingTime { get; set; }
    public double AveragePingTime { get; set; }
    public long MaximumPingTime { get; set; }
    public double P50PingTime { get; set; }
    public double P90PingTime { get; set; }
    public double P99PingTime { get; set; }
    public double AveragePacketLossInPercentForLongTime { get; set; }
    public double AveragePacketLossInPercentForMidTime { get; set; }
    public double AveragePacketLossInPercentForShortTime { get; set; }

    public PingProfilingEvent()
    {
    }

    public PingProfilingEvent(PingAnalyzer analyzer)
    {
        CurrentPingTime = analyzer.CurrentPingTime;
        MinimumPingTime = analyzer.MinimumPingTime;
        AveragePingTime = analyzer.AveragePingTime;
        MaximumPingTime = analyzer.MaximumPingTime;
        P50PingTime = analyzer.P50PingTime;
        P90PingTime = analyzer.P90PingTime;
        P99PingTime = analyzer.P99PingTime;
        AveragePacketLossInPercentForLongTime = analyzer.AveragePacketLossInPercentForLongTime;
        AveragePacketLossInPercentForMidTime = analyzer.AveragePacketLossInPercentForMidTime;
        AveragePacketLossInPercentForShortTime = analyzer.AveragePacketLossInPercentForShortTime;
    }

    public override void SerializeData(BinaryWriter writer)
    {
        writer.Write(CurrentPingTime);
        writer.Write(MinimumPingTime);
        writer.Write(AveragePingTime);
        writer.Write(MaximumPingTime);
        writer.Write(P50PingTime);
        writer.Write(P90PingTime);
        writer.Write(P99PingTime);
        writer.Write(AveragePacketLossInPercentForLongTime);
        writer.Write(AveragePacketLossInPercentForMidTime);
        writer.Write(AveragePacketLossInPercentForShortTime);
    }

    public override void DeserializeData(BinaryReader reader)
    {
        CurrentPingTime = reader.ReadInt64();
        MinimumPingTime = reader.ReadInt64();
        AveragePingTime = reader.ReadDouble();
        MaximumPingTime = reader.ReadInt64();
        P50PingTime = reader.ReadDouble();
        P90PingTime = reader.ReadDouble();
        P99PingTime = reader.ReadDouble();
        AveragePacketLossInPercentForLongTime = reader.ReadDouble();
        AveragePacketLossInPercentForMidTime = reader.ReadDouble();
        AveragePacketLossInPercentForShortTime = reader.ReadDouble();
    }
}