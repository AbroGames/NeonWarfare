namespace KludgeBox.Events;

public abstract class HandleableEvent : IEvent
{
    public bool IsHandled { get; protected set; } = false;

    public void Handle() => IsHandled = true;
}