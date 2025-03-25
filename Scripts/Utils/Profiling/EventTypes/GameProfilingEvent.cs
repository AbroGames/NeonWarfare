using System.IO;

namespace NeonWarfare.Scripts.Utils.Profiling;

public abstract class GameProfilingEvent : ProfilingEvent
{
    
}

public enum WorldType : byte
{
    None,
    SafeWorld,
    BattleWorld
}
public class GameWorldProfilingEvent : GameProfilingEvent
{
    public WorldType WorldType { get; private set; }

    public GameWorldProfilingEvent(WorldType worldType)
    {
        WorldType = worldType;
    }

    public GameWorldProfilingEvent()
    {
    }


    public override void SerializeData(BinaryWriter writer)
    {
        writer.Write((byte)WorldType);
    }

    public override void DeserializeData(BinaryReader reader)
    {
        WorldType = (WorldType)reader.ReadByte();
    }

    public override string ToString()
    {
        return $"World Type: {WorldType}";
    }
}