namespace KludgeBox.Events;

public interface IInstanceEvent : IEvent
{
    object InstanceId { get; }
}