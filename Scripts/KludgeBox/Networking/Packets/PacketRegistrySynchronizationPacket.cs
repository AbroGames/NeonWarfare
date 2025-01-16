using System.Collections.Generic;

namespace NeonWarfare.Scripts.KludgeBox.Networking.Packets;

public sealed class PacketRegistrySynchronizationPacket : NetPacket
{
    public List<string> PacketTypesOrderedList { get; set; }
}
