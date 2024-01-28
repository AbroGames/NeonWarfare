using System;

namespace KludgeBox.Events;

internal class Listener<T> : IListener where T : IEvent
{
    private Action<T> _action;

    internal Listener(Action<T> action)
    {
        this._action = action;
    }
    
    public void Deliver(IEvent @event)
    {
        if (@event is T tEvent)
            _action?.Invoke(tEvent);
    }
}