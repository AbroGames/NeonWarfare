using System;

namespace NeonWarfare.Utils.Networking;

[AttributeUsage(AttributeTargets.Method)]
public class PacketListenerAttribute : Attribute
{
    public PacketListenerAttribute()
    {
        Side = ListenerSide.Both;
    }
    
    public PacketListenerAttribute(ListenerSide side)
    {
        Side = side;
    }

    public ListenerSide Side { get; }
}