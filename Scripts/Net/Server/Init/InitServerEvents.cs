using KludgeBox.Events;

namespace NeoVector;

public readonly record struct InitServerRequest() : IEvent;

public record GetPortFromCmdArgsQuery : QueryEvent<int>;
public record GetAdminFromCmdArgsQuery : QueryEvent<string>;