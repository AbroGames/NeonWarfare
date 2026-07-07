# Чат и команды

[← README проекта](../../README.md)

Сообщение идёт `клиент → сервер (TrySendNewMessageRpc) → перехватчики → рассылка`.
Перехватчик `IChatMessageInterceptor` может «съесть» сообщение: так `ChatMessageCommandInterceptor`
забирает всё, что начинается с `/`, и отдаёт в `WorldCommandService`.

Команды — классы, реализующие `ICommandProcessor` (`GetCommand`, `GetDescription`, `IsRequiringAdmin`,
`ProcessCommand`). Они **регистрируются автоматически**: сервис на старте сканирует сборку и собирает
все реализации. Чтобы добавить команду, достаточно создать наследника `ICommandProcessor`,
предпочтительно в `Scenes/World/Service/Command/Impl/`.

Текущий набор:

| Команда | Класс | Права |
|---|---|---|
| `/help` | `HelpCommand` | все |
| `/players` | `GetPlayersCommand` | все |
| `/uids` | `GetUidsCommand` | все |
| `/admins` | `GetAdminsCommand` | все |
| `/admin {add\|remove} <nickname>` | `ControlAdminsCommand` | админ |
| `/surface {safe\|battle}` | `ControlSurfaceCommand` | админ |

Отдельно стоит `NotFoundCommand` — заглушка для неизвестных команд. Она не имеет своего имени и
вызывается, когда ни один `ICommandProcessor` не подошёл.

> [!IMPORTANT]
> Осознанное отклонение от [правил локализации](../Localization.md): ответы чат-команд не
> локализовываются.
>
> Команды — админский инструмент, к которому обращаются крайне редко, поэтому держать их тексты в
> `Assets/Locales/*.po` не окупается. Сообщения пишутся по-английски обычными `private const string`
> в начале класса, ключи для них не заводятся.
