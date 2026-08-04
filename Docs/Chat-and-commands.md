# Chat and commands

[← Project README](../README.md)

A message travels `client → server (TrySendNewMessageRpc) → interceptors → broadcast`. An
`IChatMessageInterceptor` interceptor can "eat" a message: this is how `ChatMessageCommandInterceptor`
takes everything that starts with `/` and hands it to `WorldCommandService`.

Commands are classes implementing `ICommandProcessor` (`GetCommand`, `GetDescription`,
`IsRequiringAdmin`, `ProcessCommand`). They are **registered automatically**: at startup the service
scans the assembly and collects all the implementations. To add a command it is enough to create a
descendant of `ICommandProcessor`, preferably in `Scenes/World/Service/Command/Impl/`.

The current set:

| Command | Class | Rights |
|---|---|---|
| `/help` | `HelpCommand` | everyone |
| `/players` | `GetPlayersCommand` | everyone |
| `/uids` | `GetUidsCommand` | everyone |
| `/admins` | `GetAdminsCommand` | everyone |
| `/admin {add\|remove} <nickname>` | `ControlAdminsCommand` | admin |
| `/surface {safe\|battle}` | `ControlSurfaceCommand` | admin |

`NotFoundCommand` stands apart — a stub for unknown commands. It has no name of its own and is called
when no `ICommandProcessor` matched.

> [!IMPORTANT]
> A deliberate deviation from the [localization rules](Localization.md): command responses are **not**
> localized — a rarely used admin tool does not pay for keys in `Assets/Locales/*.po`. They are written
> in English as ordinary `private const string`s at the top of the class.
