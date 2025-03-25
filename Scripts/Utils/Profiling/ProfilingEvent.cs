using System.IO;

namespace NeonWarfare.Scripts.Utils.Profiling;

public abstract class ProfilingEvent
{
    public long Timestamp { get; set; }
    public abstract void SerializeData(BinaryWriter writer);
    public abstract void DeserializeData(BinaryReader reader);
}