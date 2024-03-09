using KludgeBox.Events;

namespace NeoVector;

public readonly record struct CreateServerRequest(int Port, string AdminNickname) : IEvent;
public readonly record struct ConnectToServerRequest(string Host, int Port) : IEvent;

public readonly record struct PeerConnectedClientEvent(long Id) : IEvent;
public readonly record struct PeerConnectedServerEvent(long Id) : IEvent;
public readonly record struct PeerDisconnectedClientEvent(long Id) : IEvent;
public readonly record struct PeerDisconnectedServerEvent(long Id) : IEvent;
public readonly record struct ConnectedToServerEvent : IEvent;
public readonly record struct ConnectionToServerFailedEvent : IEvent;
public readonly record struct ServerDisconnectedEvent : IEvent;