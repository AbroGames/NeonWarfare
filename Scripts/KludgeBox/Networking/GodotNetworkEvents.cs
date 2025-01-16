using NeonWarfare.Scripts.KludgeBox.Events.EventTypes;

namespace NeonWarfare.Scripts.KludgeBox.Networking;

public readonly record struct PeerConnectedEvent(long Id) : IEvent;
public readonly record struct PeerDisconnectedEvent(long Id) : IEvent;
public readonly record struct ConnectedToServerEvent : IEvent;
public readonly record struct ConnectionToServerFailedEvent : IEvent;
public readonly record struct ServerDisconnectedEvent : IEvent;
