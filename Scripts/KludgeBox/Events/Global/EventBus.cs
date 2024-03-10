using System;
using System.Linq;

namespace KludgeBox.Events.Global;

public static class EventBus
{
    private static KludgeEventBus _bus = new();

    public static ListenerSide Side
    {
        get => _bus.Side;
        set => _bus.Side = value;
    }

    public static ListenerToken Subscribe<T>(Action<T> action, ListenerPriority priority = ListenerPriority.Normal)
        where T : IEvent
    {
        return _bus.Subscribe(action, priority);
    }

    /// <summary>
    /// Publishes an event to all registered listeners.
    /// </summary>
    /// <typeparam name="T">The type of event to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    public static void Publish<T>(T @event) where T : IEvent
    {
        _bus.Publish(@event);
    }

    public static void Publish<T>() where T : IEvent, new()
    {
        _bus.Publish(new T());
    }

    /// <summary>
    /// Returns if CancellableEvent was cancelled
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static bool TryPublish(CancellableEvent @event)
    {
        return _bus.TryPublish(@event);
    }

    public static TResult Require<TResult>(QueryEvent<TResult> @event)
    {
        return _bus.Require(@event);
    }
    public static bool TryRequire<TResult>(QueryEvent<TResult> @event, out TResult result)
    {
        result = Require(@event);
        return @event.HasResult;
    }


    public static EventPublisher<T> GetEventPublisher<T>(bool track = false) where T : IEvent
    {
        return _bus.GetEventPublisher<T>(track);
    }
    
    public static QueryPublisher<T> GetQueryPublisher<T>(bool track = false) where T : QueryEvent
    {
        return _bus.GetQueryPublisher<T>(track);
    }

    public static void SubscribeMethod(MethodSubscriptionInfo subscriptionInfo)
    {
        var listenerSide = subscriptionInfo.Side;
        var busSide = Side;
        
        if (!listenerSide.HasFlag(busSide))
            return;
        
        _bus.SubscribeMethod(subscriptionInfo);
    }

    public static void RegisterListeners(ServiceRegistry registry)
    {
        var listeners = EventScanner.ScanEventListenersInTypesOfType(registry.Services.ToArray(), typeof(IEvent));

        Log.Info($"Registering {listeners.Count()} listeners from registered services");
        foreach (var listener in listeners)
        {
            SubscribeMethod(listener);
        }
    }


    /// <summary>
    /// Resets all the EventHubs.
    /// </summary>
    public static void Reset()
    {
        _bus.Reset();
    }

    /// <summary>
    /// Resets the EventHub associated with the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type for which to reset the EventHub.</typeparam>
    public static void ResetEvent<T>() where T : IEvent
    {
        _bus.ResetEvent<T>();
    }

    /// <summary>
    /// Resets the EventHub associated with the specified event type.
    /// </summary>
    public static void ResetEvent(Type type)
    {
        _bus.ResetEvent(type);
    }
}