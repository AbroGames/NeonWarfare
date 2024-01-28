namespace KludgeBox.Events;

public abstract class CancellableEvent : IEvent
{
    public bool IsCancelled { get; protected set; } = false;
    
    public void Cancel() => IsCancelled = true;
}