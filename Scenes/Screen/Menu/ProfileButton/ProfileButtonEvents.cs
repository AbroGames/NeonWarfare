using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.Menu.ProfileButton;

public readonly record struct ProfileButtonReadyEvent(ProfileButton ProfileButton) : IEvent;
public readonly record struct ProfileButtonClickEvent(ProfileButton ProfileButton) : IEvent;