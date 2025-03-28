﻿using System;
using System.Collections.Generic;
using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;
using NeonWarfare.Scripts.KludgeBox.Networking.Packets;

namespace NeonWarfare.Scripts.KludgeBox.Events;

internal sealed class EventHub
{
    public Type EventHubType { get; private set;}
    public bool IsActive { get; private set; } = true;

    private List<IListener>[] _listenersByPriority;

    public EventHub(Type type)
    {
        EventHubType = type;
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
            var tracker = new DeliveryTracker(@event);
            var handleableEvent = @event as HandleableEvent;
            var isHandleable = handleableEvent is not null;
            foreach (var priority in _listenersByPriority)
            {
                foreach (var listener in priority)
                {
                    if (isHandleable && handleableEvent.IsHandled) break;
                    listener?.Deliver(tracker);
                }
            }

            if (@event is NetPacket && !tracker.WasDelivered)
            {
                Log.Warning($"packet of type {@event.GetType()} was published but not delivered to any listener");
            }
        }
    }

    internal ListenerToken Subscribe<T>(ListenerInfo<T> info, ListenerPriority priority) where T : IEvent
    {
        var priorityListeners = _listenersByPriority[(int)priority];
        
        var subscription = new Listener<T>(info.Action);
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

    public void Deactivate()
    {
        IsActive = true;
    }
}
