namespace KludgeBox.Events;

public interface IEvent{}

internal interface IListener
{
    void Deliver(IEvent @event);
}

