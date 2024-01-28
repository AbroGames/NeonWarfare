using System;
using System.IO;

namespace KludgeBox.Net;

public class PacketReader (Packet packet)
{
    private BinaryReader _reader = new BinaryReader(new MemoryStream(packet.Data));

    public void Process() => packet.Processed = true;
    
    public sbyte ReadSByte() => _reader.ReadSByte();
    public short ReadShort() => _reader.ReadInt16();
    public int ReadInt() => _reader.ReadInt32();
    public long ReadLong() => _reader.ReadInt64();

    public byte ReadByte() => _reader.ReadByte();
    public ushort ReadUShort() => _reader.ReadUInt16();
    public uint ReadUInt() => _reader.ReadUInt32();
    public ulong ReadULong() => _reader.ReadUInt64();

    public Half ReadHalf() => _reader.ReadHalf();
    public float ReadFloat() => _reader.ReadSingle();
    public double ReadDouble() => _reader.ReadDouble();
    
    public string ReadString() => _reader.ReadString();
    public char ReadChar() => _reader.ReadChar();
    
    public char[] ReadChars(int count) => _reader.ReadChars(count);
    public byte[] ReadBytes(int count) => _reader.ReadBytes(count);
    
    public bool ReadBoolean() => _reader.ReadBoolean();
    public decimal ReadDecimal() => _reader.ReadDecimal();
}