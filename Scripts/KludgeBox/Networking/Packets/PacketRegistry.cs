using System;
using NeonWarfare.Scripts.KludgeBox.Collections;
using NeonWarfare.Scripts.KludgeBox.Core;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Packets;

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
