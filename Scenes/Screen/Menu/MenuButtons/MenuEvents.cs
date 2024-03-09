using Godot;
using KludgeBox.Events;

namespace NeoVector;

public readonly record struct CreateServerButtonReadyEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct CreateServerButtonClickEvent(CreateServerButton CreateServerButton) : IEvent;
public readonly record struct ConnectToServerButtonClickEvent(ConnectToServerButton ConnectToServerButton) : IEvent;
public readonly record struct ShutDownEvent(int ExitCode = 0) : IEvent;
public readonly record struct ChangeMenuFromButtonClickRequest(PackedScene MenuChangeTo) : IEvent;