using System;
using System.Collections.Generic;

namespace AbroDraft.Net.Packets;

public class RegistrySynchronizationPacket : AbstractPacket
{
    public List<Type> PacketTypeOrder = new();
}