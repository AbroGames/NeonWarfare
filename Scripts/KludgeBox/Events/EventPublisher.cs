namespace KludgeBox.Events;

public class EventPublisher<T> where T : IEvent
{
    private EventHub _hub;

    internal EventPublisher(EventHub hub)
    {
        _hub = hub;
    }

    public void Publish(T @event)
    {
        _hub.Publish(@event);
    }
}