using System;
using System.IO;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization.Binary.Serializers;

public class ByteHandler : PayloadHandler<byte>
{
    protected override byte ReadGeneric(BinaryReader reader) => reader.ReadByte();
    protected override void WriteGeneric(BinaryWriter writer, byte value) => writer.Write(value);
}

public class SByteHandler : PayloadHandler<sbyte>
{
    protected override sbyte ReadGeneric(BinaryReader reader) => reader.ReadSByte();
    protected override void WriteGeneric(BinaryWriter writer, sbyte value) => writer.Write(value);
}

public class Int16Handler : PayloadHandler<short>
{
    protected override short ReadGeneric(BinaryReader reader) => reader.ReadInt16();
    protected override void WriteGeneric(BinaryWriter writer, short value) => writer.Write(value);
}

public class UInt16Handler : PayloadHandler<ushort>
{
    protected override ushort ReadGeneric(BinaryReader reader) => reader.ReadUInt16();
    protected override void WriteGeneric(BinaryWriter writer, ushort value) => writer.Write(value);
}

public class Int32Handler : PayloadHandler<int>
{
    protected override int ReadGeneric(BinaryReader reader) => reader.ReadInt32();
    protected override void WriteGeneric(BinaryWriter writer, int value) => writer.Write(value);
}

public class UInt32Handler : PayloadHandler<uint>
{
    protected override uint ReadGeneric(BinaryReader reader) => reader.ReadUInt32();
    protected override void WriteGeneric(BinaryWriter writer, uint value) => writer.Write(value);
}

public class Int64Handler : PayloadHandler<long>
{
    protected override long ReadGeneric(BinaryReader reader) => reader.ReadInt64();
    protected override void WriteGeneric(BinaryWriter writer, long value) => writer.Write(value);
}

public class UInt64Handler : PayloadHandler<ulong>
{
    protected override ulong ReadGeneric(BinaryReader reader) => reader.ReadUInt64();
    protected override void WriteGeneric(BinaryWriter writer, ulong value) => writer.Write(value);
}

public class HalfHandler : PayloadHandler<Half>
{
    protected override Half ReadGeneric(BinaryReader reader) => reader.ReadHalf();
    protected override void WriteGeneric(BinaryWriter writer, Half value) => writer.Write(value);
}

public class SingleHandler : PayloadHandler<float>
{
    protected override float ReadGeneric(BinaryReader reader) => reader.ReadSingle();
    protected override void WriteGeneric(BinaryWriter writer, float value) => writer.Write(value);
}

public class DoubleHandler : PayloadHandler<double>
{
    protected override double ReadGeneric(BinaryReader reader) => reader.ReadDouble();
    protected override void WriteGeneric(BinaryWriter writer, double value) => writer.Write(value);
}

public class StringHandler : PayloadHandler<string>
{
    protected override string ReadGeneric(BinaryReader reader) => reader.ReadString();
    protected override void WriteGeneric(BinaryWriter writer, string value) => writer.Write(value);
}

public class CharHandler : PayloadHandler<char>
{
    protected override char ReadGeneric(BinaryReader reader) => reader.ReadChar();
    protected override void WriteGeneric(BinaryWriter writer, char value) => writer.Write(value);
}

public class BoolHandler : PayloadHandler<bool>
{
    protected override bool ReadGeneric(BinaryReader reader) => reader.ReadBoolean();
    protected override void WriteGeneric(BinaryWriter writer, bool value) => writer.Write(value);
}
