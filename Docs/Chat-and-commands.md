# Chat and commands

[← Project README](../README.md)

The chat is the feature `World/Features/Chat/` (see [World features](World-features.md)). It is made of events, not
of a model: nothing of it goes into the save or the join snapshot, and the history lives on each client.

1. The `Hud` sends `SendChatMessageCommand(text)` (see [Networking](Networking.md#commands-client--server)).
2. `SendChatMessageHandler` validates the text (not blank, at most 1024 characters, no control characters) and
   calls `ChatSimulationFacade.HandleInput(senderUid, text)`.
3. Text starting with `/` is a chat command (below); anything else goes to `ChatSimulation.SendMessageAsPlayerToAll`,
   which logs it and publishes `ChatPlayerMessageEvent` to everyone.
4. On every client `ChatPresentation` handles the chat events and `PlayerJoinedEvent` / `PlayerLeftEvent`, keeps the
   last 100 entries as typed facts (`ChatEntry`) and posts `ChatEntryAddedNotice` into `HudMailbox`; the `Hud` then
   re-reads the history and renders it, translating the server's facts with `Tr` — so a language switch re-renders
   the old entries too.

The sender travels as a uid through the whole chain; the nick and the rights are read by it where they are needed
(`PlayerQuery.Get(uid)`). The dedicated server has no chat window: its chat is in the process log, written by
`ChatSimulation`.

## Chat commands

A chat command is a class implementing `IChatCommand` (`Name`, `Description`, `RequiresAdmin`,
`Execute(senderUid, arguments)`) — a `[SimulationFacade]` named `*ChatCommandSimulationFacade` in
`World/Features/Chat/ChatCommands/`, since a command may change the state through other facades. The composition
root passes every created one to `ChatSimulationFacade.Register`; the name is lower case without whitespace, unique.

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
