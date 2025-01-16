namespace NeonWarfare.Scripts.KludgeBox.Events;

public enum ListenerPriority
{
    /// <summary>
    /// It is not recommended to write anything to events when using Monitor priority.
    /// Consider processing them in read-only mode or use Highest priority instead.
    /// </summary>
    Monitor,
    
    Highest,
    High,
    Normal,
    Low,
    Lowest
}
