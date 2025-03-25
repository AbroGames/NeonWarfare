using System.IO;

namespace NeonWarfare.Scripts.Utils.Profiling;

public sealed class PerformanceProfilingEvent : ProfilingEvent
{
    public long WorkingSet64 { get; set; }
    public long RenderVideoMemUsed { get; set; }
    public int NodesCount { get; set; }
    public int OrphanNodesCount { get; set; }
    public int ResourcesCount { get; set; }
    public double Fps { get; set; }
    public double Tps { get; set; }

    public PerformanceProfilingEvent(long workingSet64, long renderVideoMemUsed, int nodesCount, int orphanNodesCount, int resourcesCount, double fps, double tps)
    {
        WorkingSet64 = workingSet64;
        RenderVideoMemUsed = renderVideoMemUsed;
        NodesCount = nodesCount;
        OrphanNodesCount = orphanNodesCount;
        ResourcesCount = resourcesCount;
        Fps = fps;
        Tps = tps;
    }

    public PerformanceProfilingEvent()
    {
        
    }


    public override void SerializeData(BinaryWriter writer)
    {
        writer.Write(WorkingSet64);
        writer.Write(RenderVideoMemUsed);
        writer.Write(NodesCount);
        writer.Write(OrphanNodesCount);
        writer.Write(ResourcesCount);
        writer.Write(Fps);
        writer.Write(Tps);
    }

    public override void DeserializeData(BinaryReader reader)
    {
        WorkingSet64 = reader.ReadInt64();
        RenderVideoMemUsed = reader.ReadInt64();
        NodesCount = reader.ReadInt32();
        OrphanNodesCount = reader.ReadInt32();
        ResourcesCount = reader.ReadInt32();
        Fps = reader.ReadDouble();
        Tps = reader.ReadDouble();
    }

    public override string ToString()
    {
        const double defaultSize = 1024 * 1024 * 1024; // MiB
        return $"M:{WorkingSet64/defaultSize:N2}MiB; VM:{RenderVideoMemUsed/defaultSize:N2}MiB; N:{NodesCount}; ON:{OrphanNodesCount}; R:{ResourcesCount}";
    }
}