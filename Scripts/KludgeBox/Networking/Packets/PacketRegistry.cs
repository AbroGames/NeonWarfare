
using System;
using KludgeBox.Collections;
using KludgeBox;

namespace KludgeBox.Networking;

public class PacketRegistry : TypeRegistry
{
    public PacketRegistry() : base(typeof(NetPacket))
    {
        RegisterType(typeof(PacketRegistrySynchronizationPacket));
    }
    
    public void ScanPackets()
    {
        var packets = ReflectionExtensions.FindTypesWithAttributes(typeof(GamePacketAttribute));
        foreach (Type packet in packets)
        {
            RegisterType(packet);
        }
    }
}