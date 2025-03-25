using System;
using System.Collections.Generic;
using System.IO;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.Utils.Profiling;

public abstract class NetworkProfilingEvent : ProfilingEvent
{
    public static List<Type> PacketTypes = new ();
    
    public int PacketType { get; set; }
    public long Sender { get; set; }
    public int Size { get; set; }

    public NetworkProfilingEvent(NetPacket packet, int size)
    {
        var type = packet.GetType();
        var typeId = GetPacketTypeId(type);
        
        PacketType = typeId;
        Sender = packet.SenderId;
        Size = size;
    }

    public NetworkProfilingEvent() {}

    public static int GetPacketTypeId(Type packetType)
    {
        if (PacketTypes.Contains(packetType))
        {
            return PacketTypes.IndexOf(packetType);
        }
        
        PacketTypes.Add(packetType);
        return PacketTypes.Count - 1;
    }

    public override void SerializeData(BinaryWriter writer)
    {
        writer.Write(PacketType);
        writer.Write(Sender);
    }

    public override void DeserializeData(BinaryReader reader)
    {
        PacketType = reader.ReadInt32();
        Sender = reader.ReadInt64();
    }

    public override string ToString()
    {
        string typeName;
        if (GetType() == typeof(IncomingNetworkProfilingEvent))
        {
            typeName = "Incoming packet";
        }
        else
        {
            typeName = "Outgoing packet";
        }

        return $"{typeName} ({Size} bytes): {PacketTypes[PacketType].Name}";
    }
}


public sealed class IncomingNetworkProfilingEvent : NetworkProfilingEvent
{
    public IncomingNetworkProfilingEvent(NetPacket packet, int size) : base(packet, size) {}
}

public sealed class OutgoingNetworkProfilingEvent : NetworkProfilingEvent
{
    public OutgoingNetworkProfilingEvent(NetPacket packet, int size) : base(packet, size) { }
}