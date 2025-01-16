using NeonWarfare.Scripts.Utils.Networking.PacketBus.PacketTypes;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.BuiltinPackets;

public class PacketIdsSynchronizationPacket : IPacket
{
    public string[] OrderedPackets;
}
