namespace NeonWarfare.Scripts.KludgeBox.Events.EventTypes;

public interface IInstanceEvent : IEvent
{
    object NetworkId { get; }
}
