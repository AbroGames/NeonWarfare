namespace KludgeBox.Events;


public abstract record QueryEvent
{
    public bool HasResult { get; private set; }
    public object Response { get; private set; }
    internal virtual void SetResult(object result)
    {
        HasResult = true;
        Response = result;
    }
}
public abstract record QueryEvent<T> : QueryEvent, IEvent
{
    public void SetResult(T result)
    {
        base.SetResult(result);
    }
}