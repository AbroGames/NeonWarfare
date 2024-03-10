
using System;

namespace KludgeBox.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventListenerAttribute : Attribute
{
    public bool IsDefault { get; private init; } = false;
    public ListenerPriority Priority { get; private init; } = ListenerPriority.Normal;
    public ListenerSide Side { get; private init; } = ListenerSide.Both;
    public EventListenerAttribute(){}

    public EventListenerAttribute(ListenerPriority priority)
    {
        Priority = priority;
    }
    public EventListenerAttribute(ListenerSide side)
    {
        Side = side;
    }
    
    public EventListenerAttribute(bool isDefault)
    {
        IsDefault = isDefault;
    }
    
    public EventListenerAttribute(ListenerPriority priority, bool isDefault, ListenerSide side)
    {
        Priority = priority;
        IsDefault = isDefault;
        Side = side;
    }
}