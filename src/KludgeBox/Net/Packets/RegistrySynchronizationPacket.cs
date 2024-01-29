using System;
using System.Collections.Generic;

namespace KludgeBox.Net.Packets;

public class RegistrySynchronizationPacket : AbstractPacket
{
    public List<Type> PacketTypeOrder = new();
}