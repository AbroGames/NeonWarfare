using System;
using System.Collections.Generic;

namespace KludgeBox.Net.Packets;

public class RegistrySynchronizationPacket : AbstractPacket
{
    public List<Type> PacketTypeOrder = new();

    /// <inheritdoc />
    public override bool IsBufferable => false;

    /// <inheritdoc />
    public override bool IsReliable => true;
}