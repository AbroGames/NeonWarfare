using KludgeBox.Events;

namespace NeoVector;

public readonly record struct ProfileButtonReadyEvent(ProfileButton ProfileButton) : IEvent;
public readonly record struct ProfileButtonClickEvent(ProfileButton ProfileButton) : IEvent;