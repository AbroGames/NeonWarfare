
using System;

namespace KludgeBox.Events;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class GameEventListenerAttribute : Attribute
{
    public ListenerPriority Priority { get; private init; } = ListenerPriority.Normal;
    public GameEventListenerAttribute(){}

    public GameEventListenerAttribute(ListenerPriority priority)
    {
        Priority = priority;
    }
}