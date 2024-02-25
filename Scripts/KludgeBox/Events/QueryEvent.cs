namespace KludgeBox.Events;

public record QueryEvent<T> : IEvent
{
    public bool HasResult { get; private set; }
    public T Response { get; private set; }

    public void SetResult(T result)
    {
        HasResult = true;
        Response = result;
    }
}