using System;

namespace KludgeBox.Events;

public class EventPublisher
{
    internal EventHub hub;


    internal EventPublisher(EventHub hub)
    {
        this.hub = hub;
    }
    
    internal void Publish(IEvent @event)
    {
        if (!hub.IsActive)
            throw new InvalidOperationException($"This publisher's hub is inactive");
            
        hub.Publish(@event);
    }

    internal void ReassignHub(EventHub hub)
    {
        this.hub = hub;
    }

    public EventPublisher<T> AsGeneric<T>() where T : IEvent
    {
        if (typeof(T) != hub.EventHubType)
            throw new ArgumentException($"Attempt to create generic publisher of type {typeof(T)} for {hub.EventHubType} hub.");
        
        return new EventPublisher<T>(this);
    }
}

public sealed class EventPublisher<T> where T : IEvent
{
    private EventPublisher _publisher;
    public bool IsActive => _publisher.hub.IsActive;

    internal EventPublisher(EventPublisher publisher)
    { 
        _publisher = publisher;
    }
    

    public void Publish(T @event)
    {
        if (!_publisher.hub.IsActive)
            throw new InvalidOperationException($"This publisher's hub ({typeof(T).Name}) is inactive");
            
        _publisher.hub.Publish(@event);
    }
}