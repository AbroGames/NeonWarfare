using Godot;
using NeonWarfare.Utils.InstanceRouting;

namespace NeonWarfare.Scripts.Utils.Networking;

public class PacketProcessingUnit
{
    public PacketBus PacketBus { get; set; }
    public PacketSerializer PacketSerializer { get; set; }
    public PacketRegistry PacketRegistry { get; set; }
    public InstanceRouter InstanceRouter { get; set; }
    
    public PacketProcessingUnit(PacketBus bus, PacketRegistry registry, PacketSerializer packetSerializer, InstanceRouter instanceRouter)
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
            PacketBus = new PacketBus();
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
