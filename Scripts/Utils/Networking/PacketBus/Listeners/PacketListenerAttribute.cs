using System;

namespace NeonWarfare.Scripts.Utils.Networking.PacketBus.Listeners;

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
