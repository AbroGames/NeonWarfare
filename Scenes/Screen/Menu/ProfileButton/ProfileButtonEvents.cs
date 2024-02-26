using KludgeBox.Events;

namespace AbroDraft;

public readonly record struct ProfileButtonReadyEvent(ProfileButton ProfileButton) : IEvent;
public readonly record struct ProfileButtonClickEvent(ProfileButton ProfileButton) : IEvent;