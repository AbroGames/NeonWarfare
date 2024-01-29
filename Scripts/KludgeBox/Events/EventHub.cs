using System;
using System.Collections.Generic;

namespace KludgeBox.Events;

internal sealed class EventHub
{
    //private List<IListener> _listeners = new();

    private List<IListener>[] _listenersByPriority;

    public EventHub()
    {
        var prioritiesCount = Enum.GetValues(typeof(ListenerPriority)).Length;
        _listenersByPriority = new List<IListener>[prioritiesCount];
        for (int i = 0; i < prioritiesCount; i++)
        {
            _listenersByPriority[i] = new();
        }
    }
    
    internal void Publish<T>(T @event) where T : IEvent
    {
        if (@event is not null)
        {
            var handleableEvent = @event as HandleableEvent;
            var isHandleable = handleableEvent is not null;
            foreach (var priority in _listenersByPriority)
            {
                foreach (var listener in priority)
                {
                    if (isHandleable && handleableEvent.IsHandled) break;
                    listener?.Deliver(@event);
                }
            }
            
        }
    }

    internal ListenerToken Subscribe<T>(Action<T> action, ListenerPriority priority) where T : IEvent
    {
        var priorityListeners = _listenersByPriority[(int)priority];
        
        var subscription = new Listener<T>(action);
        priorityListeners.Add(subscription);
        var token = new ListenerToken(subscription, this);
        return token;
    }

    internal void Unsubscribe(ListenerToken token)
    {
        foreach (var listeners in _listenersByPriority)
        {
            listeners.Remove(token.Listener);
        }
    }

    public void Reset()
    {
        var prioritiesCount = Enum.GetValues(typeof(ListenerPriority)).Length;
        _listenersByPriority = new List<IListener>[prioritiesCount];
    }
}