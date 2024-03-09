using KludgeBox.Events;

namespace NeoVector;

public readonly record struct PlayerInfoReadyEvent(PlayerInfo PlayerInfo) : IEvent;