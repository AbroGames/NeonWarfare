namespace KludgeBox.Events;

public interface IInstanceEvent : IEvent
{
    object NetworkId { get; }
}