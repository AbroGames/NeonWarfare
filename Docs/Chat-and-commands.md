# Chat and commands

[← Project README](../README.md)

The chat is the feature `Worlds/Features/Chat/` (see [World features](World-features.md)). It is made of events, not
of a model: nothing of it goes into the save or the join snapshot, and the history lives on each client.

1. The `Hud` sends `SendChatMessageCommand(text)` (see [Networking](Networking.md#commands-client--server)).
2. `SendChatMessageHandler` validates the text (not blank, at most 1024 characters, no control characters) and
   calls `ChatSimulationFacade.HandleInput(senderUid, text)`.
3. Text starting with `/` is a chat command (below); anything else goes to `ChatSimulation.SendMessageAsPlayerToAll`,
   which logs it and publishes `ChatPlayerMessageEvent` to everyone.
4. On every client `ChatPresentation` handles the chat events, keeps the last 100 entries as typed facts
   (`ChatEntry`) and posts `ChatEntryAddedNotice` into `HudMailbox`; the `Hud` then re-reads the history and renders
   it, translating the server's messages with `Tr` — so a language switch re-renders the old entries too.

The sender travels as a uid through the whole chain; the nick and the rights are read by it where they are needed
(`PlayerQuery.Get(uid)`). The dedicated server has no chat window: its chat is in the process log, written by
`ChatSimulation`.

## Localized server messages

A server message the player sees — a join, a leave, the save result — is
`ChatSimulation.SendLocalizedMessageAsServerToAll` / `…ToPlayer(key, args)` → `LocalizedChatMessageEvent(key,
string[] args)`: the server cannot call `Tr`, it would translate into its own language. The key is a `const string`
at the publisher, so `LocalizationUsageTests` checks it against `Assets/Locales`; the arguments fill `{0}`, `{1}`, …
of the translation. The `Hud` prefixes every such line with the server nick; a translation the arguments do not fit
shows as the key and the arguments, as does an unknown key.

## Chat commands

A chat command is a class implementing `IListedChatCommand` (`Name`, `Description`, `RequiresAdmin`,
`Execute(senderUid, arguments)`) — a `[SimulationFacade]` named `*ChatCommandSimulationFacade` in
`Worlds/Features/Chat/ChatCommands/`, since a command may change the state through other facades.
`ChatSimulationFacade` takes every `IChatCommand` in its constructor; the name is lower case without whitespace,
unique. `/help` takes every `IListedChatCommand`, so it implements only the base `IChatCommand` and does not list
itself.

`ChatSimulationFacade` logs the command, splits the name from the arguments and runs it. An unknown name or missing
admin rights get a reply instead; a reply is `ChatSimulation.SendMessageAsServerToPlayer(text, senderUid)` →
`ChatServerMessageEvent` to the sender alone.

| Command | Class | Rights |
|---|---|---|
| `/help` | `HelpChatCommandSimulationFacade` | everyone |

> [!IMPORTANT]
> A deliberate deviation from the [localization rules](Localization.md): command replies are **not** localized —
> a rarely used admin tool does not pay for keys in `Assets/Locales/*.po`. They are written in English as ordinary
> `private const string`s at the top of the class and travel as ready text in `ChatServerMessageEvent`.
