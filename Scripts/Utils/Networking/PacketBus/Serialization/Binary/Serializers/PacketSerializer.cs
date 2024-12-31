using System;
using System.Buffers;
using System.IO;
using KludgeBox.Collections;
using KludgeBox.Networking;

namespace NeonWarfare.Utils.Networking;

public class PacketSerializer
{
    public object Deserialize(byte[] data, PacketRegistry packetRegistry)
    {
        var reader = GetReaderForData(data);
        var packetTypeId = reader.ReadInt32();
        var packetType = packetRegistry.GetTypeById(packetTypeId);
        
        var packet = Activator.CreateInstance(packetType);
        var accessors = packetType.GetSerializableMembers();

        foreach (var accessor in accessors)
        {
            var valueType = accessor.ValueType;
            if (PayloadHandler.TryGetHandler(valueType, out var handler))
            {
                accessor.SetValue(packet, handler.Read(reader));
            }
        }
        
        return packet;
    }

    public byte[] Serialize(object packet, PacketRegistry packetRegistry)
    {
        if (packet is not IPacket)
            throw new ArgumentException("Packet serializer can only serialize IPacket types");
        
        var packetType = packet.GetType();
        var packetTypeId = packetRegistry.GetTypeId(packetType);
        var accessors = packetType.GetSerializableMembers();
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);
        
        writer.Write(packetTypeId);
        foreach (var accessor in accessors)
        {
            var valueType = accessor.ValueType;
            if (PayloadHandler.TryGetHandler(valueType, out var handler))
            {
                handler.Write(writer, accessor.GetValue(packet));
            }
        }
        
        return stream.ToArray();
    }

    private static BinaryReader GetReaderForData(byte[] data)
    {
        var stream = new MemoryStream(data);
        var reader = new BinaryReader(stream);
        
        return reader;
    }
}