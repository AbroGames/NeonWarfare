using System;
using System.IO;
using System.Text;
using KludgeBox.Events;
using Newtonsoft.Json;

namespace KludgeBox.Networking;

public abstract class NetPacket : HandleableEvent
{
    [JsonIgnore]
    public long SenderId { get; internal set; }
    
    [JsonIgnore]
    public virtual int PreferredChannel => 0;
    
    
    public static NetPacket FromBuffer<TPacket>(byte[] buffer) where TPacket : NetPacket, new()
    {
        var packet = new TPacket();
        return packet.FromBuffer(buffer);
    }

    public static NetPacket FromBuffer(Type type, byte[] buffer)
    {
        var packet = Activator.CreateInstance(type) as NetPacket;
        return packet.FromBuffer(buffer);
    }
    
    /// <summary>
    /// Read packet from byte array. By default, converts to JSON and then deserializes.
    /// </summary>
    /// <remarks>
    /// It's highly recommended to write a more optimized implementation of this method. Default implementation is very inefficient.
    /// </remarks>
    /// <param name="buffer"></param>
    /// <returns>Filled packet. It's not always the same instance as the one invoked.</returns>
    public virtual NetPacket FromBuffer(byte[] buffer)
    {
        var json = Encoding.Default.GetString(buffer);
        return JsonConvert.DeserializeObject(json, GetType()) as NetPacket;
    }
    
    /// <summary>
    /// Write packet to byte array. By default, converts to JSON and then serializes.
    /// </summary>
    /// <remarks>
    /// It's highly recommended to write a more optimized implementation of this method. Default implementation is very inefficient.
    /// </remarks>
    /// <returns></returns>
    public virtual byte[] ToBuffer()
    {
        var json = JsonConvert.SerializeObject(this);
        return Encoding.Default.GetBytes(json);
    }
    
    /// <summary>
    /// Seems to work fine as is.
    /// </summary>
    /// <returns></returns>
    public virtual BinaryReader GetBinaryReader()
    {
        var stream = new MemoryStream(ToBuffer());
        var reader = new BinaryReader(stream);
        return reader;
    }
}