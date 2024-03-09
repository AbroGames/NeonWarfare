using KludgeBox.Events;

namespace NeoVector;

public record GetPortFromCmdArgsQuery : QueryEvent<int>;
public record GetAdminFromCmdArgsQuery : QueryEvent<string>;