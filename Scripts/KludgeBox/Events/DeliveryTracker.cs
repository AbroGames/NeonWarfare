using System.Collections.Generic;
using KludgeBox;
using KludgeBox.Events;

namespace NeonWarfare.Scripts.KludgeBox.Events;

internal class DeliveryTracker(IEvent @event)
{
    public IEvent Event { get; private set; } = @event;
    public int Count => _acceptedListeners.Count;
    public bool WasDelivered => Count > 0;
    
    private HashSet<IListener> _acceptedListeners = new();

    public void DeliveredTo(IListener listener)
    {
        if (!_acceptedListeners.Add(listener))
        {
            Log.Warning($"Unable to add listener {listener} to delivery tracker. Are you trying to add the same listener twice?");
        }
    }
}
