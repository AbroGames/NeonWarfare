using KludgeBox.Events;

namespace NeonWarfare.NetOld.Client;

public readonly record struct PeerConnectedClientEvent(long Id) : IEvent;
public readonly record struct PeerConnectedServerEvent(long Id) : IEvent;
public readonly record struct PeerDisconnectedClientEvent(long Id) : IEvent;
public readonly record struct PeerDisconnectedServerEvent(long Id) : IEvent;
public readonly record struct ConnectedToServerEvent : IEvent;
public readonly record struct ConnectionToServerFailedEvent : IEvent;
public readonly record struct ServerDisconnectedEvent : IEvent;