using Newtonsoft.Json;

namespace NeonWarfare.Scripts.KludgeBox.Events.EventTypes;

public abstract class HandleableEvent : IEvent
{
    [JsonIgnore]
    public bool IsHandled { get; protected set; } = false;

    public void Handle() => IsHandled = true;
}
