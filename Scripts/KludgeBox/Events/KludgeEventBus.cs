using System;
using System.Collections.Generic;
using System.Linq;

namespace KludgeBox.Events;

/// <summary>
/// Provides a central event bus for publishing and subscribing to events.
/// </summary>
public class KludgeEventBus
{
    /// <summary>
    /// If set to true, KludgeEventBus will attempt to publish events to all EventHubs whose types are derived from the event type.
    /// This option can significantly impact performance.
    /// </summary>
    public bool IncludeBaseEvents = false;

    public ListenerSide Side = ListenerSide.Both;

    private Dictionary<Type, EventHub> _hubs = new Dictionary<Type, EventHub>();

    private Dictionary<Type, EventPublisher> _publishers = new();

    /// <summary>
    /// Subscribes a listener to the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type to subscribe to.</typeparam>
    /// <param name="action">The action to execute when the event is published.</param>
    /// <returns>A listener token that can be used to unsubscribe from the event.</returns>
    public ListenerToken Subscribe<T>(Action<T> action, ListenerPriority priority) where T : IEvent
    {
        return SubscribeByInfo<T>(new(action, false, null, false), priority);
    }

    public ListenerToken SubscribeByInfo<T>(ListenerInfo<T> info, ListenerPriority priority) where T : IEvent
    {
        return GetHub(typeof(T)).Subscribe(info, priority);
    }

    /// <summary>
    /// Publishes an event to all registered listeners.
    /// </summary>
    /// <typeparam name="T">The type of event to publish.</typeparam>
    /// <param name="event">The event to publish.</param>
    public void Publish<T>(T @event) where T : IEvent
    {
        if (IncludeBaseEvents)
        {
            foreach (var hub in FindApplicableHubs(@event.GetType()))
            {
                hub.Publish(@event);
            }
        }
        else
        {
            GetHub(@event.GetType()).Publish(@event);
        }
    }

    /// <summary>
    /// Returns if CancellableEvent was cancelled
    /// </summary>
    /// <param name="event"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public bool TryPublish(CancellableEvent @event)
    {
        Publish(@event);
        return @event.IsCancelled;
    }

    public TResult Require<TResult>(QueryEvent<TResult> @event)
    {
        Publish(@event);
        return @event.Result;
    }

    public bool TryRequire<TResult>(QueryEvent<TResult> @event, out TResult result)
    {
        result = Require(@event);
        return @event.HasResult;
    }

    public EventPublisher<T> GetEventPublisher<T>(bool track = false) where T : IEvent
    {
        var type = typeof(T);
        var publisher = GetPublisher(type, track);
        EventPublisher<T> genericPublisher = publisher.AsGeneric<T>();
        return genericPublisher;
    }
    
    public QueryPublisher<T> GetQueryPublisher<T>(bool track = false) where T : QueryEvent
    {
        var type = typeof(T);
        var publisher = GetPublisher(type, track);
        return new QueryPublisher<T>(publisher);
    }

    internal EventPublisher GetPublisher(Type type, bool track = false)
    {
        EventPublisher publisher;

        if (track)
        {
            if (_publishers.TryGetValue(type, out var cachedPublisher))
            {
                publisher = cachedPublisher;
            }
            else
            {
                publisher = new EventPublisher(GetHub(type));
                _publishers[type] = publisher;
            }
        }
        else
        {
            publisher = new EventPublisher(GetHub(type));
        }
        
        return publisher;
    }


    /// <summary>
    /// Resets all the EventHubs.
    /// </summary>
    public void Reset(bool hard = false)
    {
        foreach ((Type _, EventHub eventHub) in _hubs)
        {
            eventHub.Deactivate();
        }
        
        _hubs.Clear();
        
        if (hard)
        {
            _publishers.Clear();
        }
        else
        {
            foreach ((Type key, EventPublisher value) in _publishers)
            {
                value.ReassignHub(GetHub(key));
            }
        }
    }

    /// <summary>
    /// Resets the EventHub associated with the specified event type.
    /// </summary>
    /// <typeparam name="T">The event type for which to reset the EventHub.</typeparam>
    public void ResetEvent<T>() where T : IEvent
    {
        GetHub(typeof(T)).Reset();
    }
    
    /// <summary>
    /// Resets the EventHub associated with the specified event type.
    /// </summary>
    public void ResetEvent(Type type)
    {
        if (!type.IsAssignableTo(typeof(IEvent))) throw new ArgumentException("Provided type does not implement IEvent");
        GetHub(type).Reset();
    }

    private EventHub GetHub(Type eventType)
    {
        if (_hubs.TryGetValue(eventType, out EventHub hub) && hub is not null)
        {
            return hub;
        }

        hub = new EventHub(eventType);
        _hubs[eventType] = hub;

        return hub;
    }
    
    /// <summary>
    ///	Subscribes to a message type using the provided MethodInfo.
    /// </summary>
    /// <param name="subscriptionInfo">The MethodInfo representing the delivery action.</param>
    /// <returns>Message subscription token that can be used for unsubscribing.</returns>
    public ListenerToken SubscribeMethod(MethodSubscriptionInfo subscriptionInfo)
    {
        Delegate actionDelegate;

        Type eventType = subscriptionInfo.Method.GetParameters()[0].ParameterType;
        var delegateType = typeof(Action<>).MakeGenericType(eventType);
        var mustBeResolved = subscriptionInfo.MustBeResolved;//eventType.IsAssignableTo(typeof(IInstanceEvent));

        // Create an Action<TArg> delegate from the MethodInfo
        if (DelegateHelpers.IsQueryEvent(eventType) && DelegateHelpers.IsQueryListener(subscriptionInfo.Method))
        {
            actionDelegate = mustBeResolved
            ? DelegateHelpers.FuncToResolvingAction(subscriptionInfo.Method, eventType)
            : DelegateHelpers.FuncToAction(subscriptionInfo.Invoker, subscriptionInfo.Method);
        }
        else
        {
            actionDelegate = mustBeResolved 
                ? DelegateHelpers.InstanceResolvingAction(subscriptionInfo.Method, eventType)
                : Delegate.CreateDelegate(delegateType, subscriptionInfo.Invoker, subscriptionInfo.Method);
        }

        var infoType = typeof(ListenerInfo<>).MakeGenericType(eventType);
        var forceDefault = subscriptionInfo.Method.DeclaringType.Assembly == typeof(KludgeEventBus).Assembly;
        var info = infoType.GetConstructors()
            .First()
            .Invoke([
            actionDelegate,
            subscriptionInfo.IsDefault || forceDefault, // define by assembly it located in
            subscriptionInfo.Method, // if this is not null
            subscriptionInfo.MustBeResolved
        ]);

        Log.Debug(
            $"\tRegistered listener {subscriptionInfo.Method.Name} from {subscriptionInfo.Method.DeclaringType.Name} " +
            $"{(forceDefault ? "and forced default" : "")}");
        // Subscribe to the message type using the created delegate
        return typeof(KludgeEventBus).GetMethod("SubscribeByInfo")!.MakeGenericMethod(eventType)
            .Invoke(this, new object[] { info, subscriptionInfo.Priority }) as ListenerToken;
    }

    private List<EventHub> FindApplicableHubs(Type eventType)
    {
        List<EventHub> applicableHubs = new List<EventHub>();

        foreach (var kv in _hubs)
        {
            if (kv.Key.IsAssignableFrom(eventType))
            {
                applicableHubs.Add(kv.Value);
            }
        }

        if (applicableHubs.Count == 0)
        {
            applicableHubs.Add(GetHub(eventType));
        }

        return applicableHubs;
    }
}