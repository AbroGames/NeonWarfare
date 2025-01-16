using System.IO;
using Godot;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization.Binary.Serializers;

public class Vector2Handler : PayloadHandler<Vector2>
{
    protected override Vector2 ReadGeneric(BinaryReader reader)
    {
        var x = reader.ReadReal();
        var y = reader.ReadReal();
        
        return new Vector2(x, y);
    }

    protected override void WriteGeneric(BinaryWriter writer, Vector2 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
    }
}

public class Vector3Handler : PayloadHandler<Vector3>
{
    protected override Vector3 ReadGeneric(BinaryReader reader)
    {
        var x = reader.ReadReal();
        var y = reader.ReadReal();
        var z = reader.ReadReal();
        
        return new Vector3(x, y, z);
    }

    protected override void WriteGeneric(BinaryWriter writer, Vector3 value)
    {
        writer.Write(value.X);
        writer.Write(value.Y);
        writer.Write(value.Z);
    }
}

public class ColorHandler : PayloadHandler<Color>
{
    protected override Color ReadGeneric(BinaryReader reader)
    {
        var rgba = reader.ReadUInt64();
        return new Color(rgba);
    }

    protected override void WriteGeneric(BinaryWriter writer, Color value)
    {
        writer.Write(value.ToRgba64());
    }
}

internal static class BinaryExtensions
{
    public static real ReadReal(this BinaryReader reader)
    {
#if GODOT_REAL_T_IS_DOUBLE
        return reader.ReadDouble();
#else
        return reader.ReadSingle();
#endif
    }
}
