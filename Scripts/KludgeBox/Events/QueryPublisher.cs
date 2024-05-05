namespace KludgeBox.Events;

public sealed class QueryPublisher<T> where T : QueryEvent
{
    private EventPublisher _publisher;
    public bool IsActive => _publisher.hub.IsActive;

    internal QueryPublisher(EventPublisher publisher)
    { 
        _publisher = publisher;
    }
    

    public TResult Require<TResult>(QueryEvent<TResult> @event)
    {
        _publisher.hub.Publish(@event);
        return @event.Result;
    }
}