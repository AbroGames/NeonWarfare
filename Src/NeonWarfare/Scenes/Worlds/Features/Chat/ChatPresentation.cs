using System.Collections.Generic;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Presentation;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[Presentation]
public class ChatPresentation(HudMailbox hudMailbox) : IEventHandlerOwner
{
    private const int MaxNumberOfMessages = 100;

    public IReadOnlyCollection<ChatEntry> Entries => _entries;
    private readonly Queue<ChatEntry> _entries = new(MaxNumberOfMessages);

    /// <summary>
    /// A chat history entry. It keeps a fact, not a translated string, see Docs/Chat-and-commands.md.
    /// The replies of chat commands are not localized, see Docs/Localization.md#locale-files.
    /// </summary>
    public abstract record ChatEntry(long SentAtUnixSeconds);
    public record PlayerMessageEntry(long SentAtUnixSeconds, string SenderUid, string SenderNick, string Text)
        : ChatEntry(SentAtUnixSeconds);
    public record ServerTextEntry(long SentAtUnixSeconds, string Text) : ChatEntry(SentAtUnixSeconds);

    public record LocalizedServerEntry(long SentAtUnixSeconds, LocalizedText Text)
        : ChatEntry(SentAtUnixSeconds);

    [EventHandler]
    private void Handle(ChatPlayerMessageEvent e) =>
        AddChatEntry(new PlayerMessageEntry(e.SentAtUnixSeconds, e.SenderUid, e.SenderNick, e.Text));

    [EventHandler]
    private void Handle(ChatServerMessageEvent e) =>
        AddChatEntry(new ServerTextEntry(e.SentAtUnixSeconds, e.Text));

    [EventHandler]
    private void Handle(LocalizedChatMessageEvent e) =>
        AddChatEntry(new LocalizedServerEntry(e.SentAtUnixSeconds, e.Text));

    private void AddChatEntry(ChatEntry chatEntry)
    {
        if (_entries.Count >= MaxNumberOfMessages) _entries.Dequeue();
        _entries.Enqueue(chatEntry);
        hudMailbox.Post(new ChatEntryAddedNotice());
    }
}
