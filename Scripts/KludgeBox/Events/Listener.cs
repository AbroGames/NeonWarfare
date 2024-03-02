using System;
using NeoVector.KludgeBox.Events;

namespace KludgeBox.Events;

internal class Listener<T> : IListener where T : IEvent
{
    public bool IsDefault { get; init; }
    public ListenerInfo<T> Info { get; init; }
    
    private Action<T> _action;

    internal Listener(ListenerInfo<T> info)
    {
        _action = info.Action;
        IsDefault = info.IsDefault;
        Info = info;
    }
    
    internal Listener(Action<T> action, bool isDefault = false)
    {
        this._action = action;
        IsDefault = isDefault;
    }
    
    public void Deliver(IEvent @event)
    {
        if (@event is T tEvent)
            _action?.Invoke(tEvent);
    }
    
}