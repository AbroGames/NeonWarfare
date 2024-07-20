using System.Collections.Generic;

namespace KludgeBox.Networking;

public sealed class PacketRegistrySynchronizationPacket : NetPacket
{
    public List<string> PacketTypesOrderedList { get; set; }
}