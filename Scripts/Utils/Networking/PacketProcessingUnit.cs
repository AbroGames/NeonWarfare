using NeonWarfare.Scripts.Utils.InstanceRouting;
using NeonWarfare.Scripts.Utils.Networking.PacketBus;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners.ListenerSources;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.PacketTypes;
using NeonWarfare.Scripts.Utils.Networking.PacketBus.Serialization.Binary.Serializers;

namespace NeonWarfare.Scripts.Utils.Networking;

public class PacketProcessingUnit
{
    public PacketBus.PacketBus PacketBus { get; set; }
    public PacketSerializer PacketSerializer { get; set; }
    public PacketRegistry PacketRegistry { get; set; }
    public InstanceRouter InstanceRouter { get; set; }
    
    public PacketProcessingUnit(PacketBus.PacketBus bus, PacketRegistry registry, PacketSerializer packetSerializer, InstanceRouter instanceRouter)
    {
        InstanceRouter = instanceRouter ?? new InstanceRouter();
        PacketBus = bus;
        PacketRegistry = registry;
        PacketSerializer = packetSerializer ?? new PacketSerializer();

        if (PacketRegistry is null)
        {
            PacketRegistry = new PacketRegistry();
            PacketRegistry.Reset();
        }

        if (PacketBus is null)
        {
            PacketBus = new PacketBus.PacketBus();
            PacketBus.SubscribeAll(new SidedListenerSource(ListenerSide.Both));
        }
    }

    public void ProcessReceivedPacket(long senderId, byte[] data)
    {
        var packet = PacketSerializer.Deserialize(data, PacketRegistry);
        PacketBus.AcceptPacket((IPacket) packet);
    }

    public byte[] GetRawData(IPacket packet)
    {
        var data = PacketSerializer.Serialize(packet, PacketRegistry);
        return data;
    }
}
