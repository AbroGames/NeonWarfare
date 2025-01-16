using System;
using NeonWarfare.KludgeBox.Events;

namespace NeonWarfare.Scripts.KludgeBox.Events;

internal class Listener<T> : IListener where T : IEvent
{
    public bool IsDefault { get; init; }
    public ListenerInfo<T> Info { get; init; }
    
    private Action<T> _action;
    private static int _nextId = 0;

    private int _id;
    private long _delivers;
    internal Listener(ListenerInfo<T> info)
    {
        _action = info.Action;
        IsDefault = info.IsDefault;
        Info = info;
        _id = _nextId++;
    }
    
    internal Listener(Action<T> action, bool isDefault = false)
    {
        this._action = action;
        IsDefault = isDefault;
    }
    
    public void Deliver(DeliveryTracker tracker)
    {
        var @event = tracker.Event;
        if (@event is T tEvent)
        {
            tracker.DeliveredTo(this);
            _action?.Invoke(tEvent);
        }
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"<Listener#{_id} for {Info} ({_delivers} deliveries)>";
    }
}
