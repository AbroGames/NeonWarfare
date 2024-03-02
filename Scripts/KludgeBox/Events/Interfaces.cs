namespace KludgeBox.Events;

internal interface IListener
{
    void Deliver(IEvent @event);
}

