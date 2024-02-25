using System.Collections.Generic;
using KludgeBox.Net.Packets;

namespace KludgeBox.Net;

public sealed class PacketBuffer
{
    private Queue<AbstractPacket> packets = new();

    public void EnqueuePacket(AbstractPacket packet)
    {
        packets.Enqueue(packet);
    }

    
    public IEnumerable<AbstractPacket> EnumeratePackets()
    {
        if (packets.Count > 0)
        {
            yield return packets.Dequeue();
        }
        else
        {
            yield break;
        }
    }
}