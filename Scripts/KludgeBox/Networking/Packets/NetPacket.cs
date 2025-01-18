using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using NeonWarfare.Scripts.KludgeBox.Core;
using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;
using Newtonsoft.Json;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Packets;

public abstract class NetPacket : HandleableEvent
{
    [JsonIgnore]
    public long SenderId { get; internal set; }
    
    /*
     * The default channel with index 0 is actually work as three separated channels - one for each transfer mode. (c) Godot Docs
     * Так что этот параметр важен только для TransferModeEnum.Reliable каналов
     */
    [JsonIgnore]
    public virtual int PreferredChannel => 0;
    
    [JsonIgnore]
    public virtual MultiplayerPeer.TransferModeEnum Mode => MultiplayerPeer.TransferModeEnum.Reliable;
    
    
    public static NetPacket FromBuffer<TPacket>(byte[] buffer, PacketRegistry packetRegistry) where TPacket : NetPacket, new()
    {
        var packet = new TPacket();
        return packet.FromBuffer(buffer, packetRegistry);
    }

    public static NetPacket FromBuffer(Type type, byte[] buffer, PacketRegistry packetRegistry)
    {
        NetPacket packet;
        if (type.HasParameterlessConstructor())
        {
            packet = Activator.CreateInstance(type) as NetPacket;
        }
        else
        {
            var firstAvailableConstructor = type.GetConstructors()[0];
            var neededParams = firstAvailableConstructor.GetParameters();
            List<object> args = new();
            
            foreach (var param in neededParams)
            {
                if (param.ParameterType == typeof(string))
                {
                    args.Add("");
                    continue;
                }
                
                args.Add(Activator.CreateInstance(param.ParameterType));
            }
            
            packet = Activator.CreateInstance(type, args.ToArray()) as NetPacket;
        }
        return packet.FromBuffer(buffer, packetRegistry);
    }
    
    /// <summary>
    /// Read packet from byte array. By default, converts to JSON and then deserializes.
    /// </summary>
    /// <remarks>
    /// It's highly recommended to write a more optimized implementation of this method. Default implementation is very inefficient.
    /// </remarks>
    /// <param name="buffer"></param>
    /// <returns>Filled packet. It's not always the same instance as the one invoked.</returns>
    public virtual NetPacket FromBuffer(byte[] buffer, PacketRegistry packetRegistry)
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
    public virtual byte[] ToBuffer(PacketRegistry packetRegistry)
    {
        var json = JsonConvert.SerializeObject(this);
        return Encoding.Default.GetBytes(json);
    }
    
    /// <summary>
    /// Seems to work fine as is.
    /// </summary>
    /// <returns></returns>
    public virtual BinaryReader GetBinaryReader(PacketRegistry packetRegistry)
    {
        var stream = new MemoryStream(ToBuffer(packetRegistry));
        var reader = new BinaryReader(stream);
        return reader;
    }
}
