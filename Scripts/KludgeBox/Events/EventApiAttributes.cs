
using System;

namespace KludgeBox.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventListenerAttribute : Attribute
{
    public bool IsDefault { get; private init; } = false;
    public ListenerPriority Priority { get; private init; } = ListenerPriority.Normal;
    public EventListenerAttribute(){}

    public EventListenerAttribute(ListenerPriority priority)
    {
        Priority = priority;
    }
    
    public EventListenerAttribute(ListenerPriority priority, bool isDefault)
    {
        Priority = priority;
        IsDefault = isDefault;
    }
    
    public EventListenerAttribute(bool isDefault)
    {
        IsDefault = isDefault;
    }
}