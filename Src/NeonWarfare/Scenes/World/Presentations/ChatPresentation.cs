using System.Collections.Generic;
using NeonWarfare.Scenes.World.Composition;
using NeonWarfare.Scenes.World.Events;

namespace NeonWarfare.Scenes.World.Presentations;

[Presentation(RequiredByServerHud = true)]
public class ChatPresentation
{
    private const int MaxNumberOfMessages = 100;

    public IReadOnlyCollection<ChatEntry> Entries => _entries;
    private readonly Queue<ChatEntry> _entries = new(MaxNumberOfMessages);

    // History keeps facts, not localized strings: the HUD translates them on render via Tr,
    // so a language switch re-renders old entries too
    // Command responses are deliberately not localized, see Docs/Localization.md
    public abstract record ChatEntry(long SentAtUnixSeconds);
    public record PlayerMessageEntry(long SentAtUnixSeconds, string SenderUid, string SenderNick, string Text)
        : ChatEntry(SentAtUnixSeconds);
    public record ServerTextEntry(long SentAtUnixSeconds, string Text) : ChatEntry(SentAtUnixSeconds);
    public record PlayerJoinedEntry(long SentAtUnixSeconds, string Uid, string Nick) : ChatEntry(SentAtUnixSeconds);
    public record PlayerLeftEntry(long SentAtUnixSeconds, string Uid, string Nick) : ChatEntry(SentAtUnixSeconds);

    //TODO [EventHandler]
    private void Handle(ChatPlayerMessageEvent e) =>
        AddChatEntry(new PlayerMessageEntry(e.SentAtUnixSeconds, e.SenderUid, e.SenderNick, e.Text));

    //TODO [EventHandler]
    private void Handle(ChatServerMessageEvent e) =>
        AddChatEntry(new ServerTextEntry(e.SentAtUnixSeconds, e.Text));

    //TODO [EventHandler]
    private void Handle(PlayerJoinedEvent e) =>
        AddChatEntry(new PlayerJoinedEntry(e.SentAtUnixSeconds, e.Uid, e.Nick));

    //TODO [EventHandler]
    private void Handle(PlayerLeftEvent e) =>
        AddChatEntry(new PlayerLeftEntry(e.SentAtUnixSeconds, e.Uid, e.Nick));

    private void AddChatEntry(ChatEntry chatEntry)
    {
        if (_entries.Count >= MaxNumberOfMessages) _entries.Dequeue();
        _entries.Enqueue(chatEntry);
        //TODO Mailbox.push(ChatMailboxEvent) Просто факт, что есть новая запись, HUD сам прочтет её отсюда
    }
}
