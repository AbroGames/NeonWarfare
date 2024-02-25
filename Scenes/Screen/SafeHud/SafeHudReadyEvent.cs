using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.SafeHud;

public readonly struct SafeHudReadyEvent(SafeHud safeHud) : IEvent
{
    public SafeHud SafeHud { get; } = safeHud;
}