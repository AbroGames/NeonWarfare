using KludgeBox.Events;

namespace NeoVector;

public readonly record struct CreateServerRequest(int Port, string AdminNickname) : IEvent;
public readonly record struct ConnectToServerRequest(string Host, int Port) : IEvent;