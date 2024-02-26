
using System;

namespace KludgeBox.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class EventListenerAttribute : Attribute
{
    public ListenerPriority Priority { get; private init; } = ListenerPriority.Normal;
    public EventListenerAttribute(){}

    public EventListenerAttribute(ListenerPriority priority)
    {
        Priority = priority;
    }
}