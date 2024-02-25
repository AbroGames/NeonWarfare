using Newtonsoft.Json;

namespace KludgeBox.Events;

public abstract class HandleableEvent : IEvent
{
    [JsonIgnore]
    public bool IsHandled { get; protected set; } = false;

    public void Handle() => IsHandled = true;
}