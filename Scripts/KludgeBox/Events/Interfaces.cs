using NeonWarfare.KludgeBox.Events;

namespace KludgeBox.Events;

internal interface IListener
{
    void Deliver(DeliveryTracker tracker);
}

