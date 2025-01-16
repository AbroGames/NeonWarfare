using NeonWarfare.KludgeBox.Events;

namespace NeonWarfare.Scripts.KludgeBox.Events;

internal interface IListener
{
    void Deliver(DeliveryTracker tracker);
}

