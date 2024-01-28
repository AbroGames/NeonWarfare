using System;
using System.IO;

namespace KludgeBox.Net;

public class DataWriter
{
    private MemoryStream _stream;
    private BinaryWriter _writer;
    
    public DataWriter()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream);
    }

    public byte[] Data => _stream.ToArray();

    public void Write(byte[] value) => _writer.Write(value);
    
    public void Write(sbyte value) => _writer.Write(value);
    public void Write(short value) => _writer.Write(value);
    public void Write(int value) => _writer.Write(value);
    public void Write(long value) => _writer.Write(value);
    
    public void Write(byte value) => _writer.Write(value);
    public void Write(ushort value) => _writer.Write(value);
    public void Write(uint value) => _writer.Write(value);
    public void Write(ulong value) => _writer.Write(value);
    
    public void Write(Half value) => _writer.Write(value);
    public void Write(float value) => _writer.Write(value);
    public void Write(double value) => _writer.Write(value);
    
    public void Write(bool value) => _writer.Write(value);
    
    public void Write(char value) => _writer.Write(value);
    public void Write(char[] value) => _writer.Write(value);
    public void Write(string value) => _writer.Write(value);
}