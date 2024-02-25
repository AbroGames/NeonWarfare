using KludgeBox.Events;

namespace AbroDraft.Scenes.Screen.Menu.CreateServerButton;

public readonly record struct CreateServerButtonReadyEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct CreateServerButtonClickEvent(CreateServerButton CreateServerButton) : IEvent;