using System;

namespace NeonWarfare.Scripts.KludgeBox.Events;

public sealed class ListenerToken
{
    internal IListener Listener { get; private set; }

    internal WeakReference<EventHub> Hub { get; private set; }
    
    public bool IsValid => Listener is not null && Hub is not null;

    internal ListenerToken(IListener action, EventHub hub)
    {
        Listener = action;
        Hub = new(hub);
    }

    
    public void Unsubscribe()
    {
        if (!IsValid)
            return;
        
        if(Hub.TryGetTarget(out var target))
            target.Unsubscribe(this);

        Hub = null;
        Listener = null;
    }
}
