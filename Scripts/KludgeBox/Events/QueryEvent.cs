namespace KludgeBox.Events;


public abstract record QueryEvent
{
    public bool HasResult { get; private set; }
    protected object Value { get; private set; }
    internal virtual void SetResult(object result)
    {
        HasResult = true;
        Value = result;
    }
}
public abstract record QueryEvent<T> : QueryEvent, IEvent
{
    public T Result => (T)Value;
    public void SetResult(T result)
    {
        base.SetResult(result);
    }
}