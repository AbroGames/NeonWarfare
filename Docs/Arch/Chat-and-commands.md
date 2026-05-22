# Чат и команды

[← README проекта](../../README.md)

Сообщение идёт `клиент → сервер (TrySendNewMessageRpc) → перехватчики → рассылка`.
Перехватчик `IChatMessageInterceptor` может «съесть» сообщение: так `ChatMessageCommandInterceptor`
забирает всё, что начинается с `/`, и отдаёт в `WorldCommandService`.

Команды — классы, реализующие `ICommandProcessor` (`GetCommand`, `GetDescription`, `IsRequiringAdmin`,
`ProcessCommand`). Они **регистрируются автоматически**: сервис на старте сканирует сборку и собирает все
реализации. Чтобы добавить команду, достаточно создать наследника `ICommandProcessor`, 
предпочтительно в `Scenes/World/Service/Command/Impl/`.

Текущий набор: `/help`, `/players`, `/uids`, `/admins`, `/admin {add|remove} <nickname>` (админ),
`/surface {safe|battle}` (админ).
