using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Worlds.Infra.Client.Events;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Presentation;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

[Presentation]
public class ChatPresentation(HudMailbox hudMailbox)
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

    public sealed record LocalizedServerEntry(long SentAtUnixSeconds, string Key, string[] Args)
        : ChatEntry(SentAtUnixSeconds)
    {
        // The generated equality compares the array by reference
        public bool Equals(LocalizedServerEntry other) =>
            other is not null
            && SentAtUnixSeconds == other.SentAtUnixSeconds
            && Key == other.Key
            && Args.SequenceEqual(other.Args);

        public override int GetHashCode() => HashCode.Combine(SentAtUnixSeconds, Key, Args.Length);
    }

    [EventHandler]
    private void Handle(ChatPlayerMessageEvent e) =>
        AddChatEntry(new PlayerMessageEntry(e.SentAtUnixSeconds, e.SenderUid, e.SenderNick, e.Text));

    [EventHandler]
    private void Handle(ChatServerMessageEvent e) =>
        AddChatEntry(new ServerTextEntry(e.SentAtUnixSeconds, e.Text));

    [EventHandler]
    private void Handle(LocalizedChatMessageEvent e) =>
        AddChatEntry(new LocalizedServerEntry(e.SentAtUnixSeconds, e.Key, e.Args));

    private void AddChatEntry(ChatEntry chatEntry)
    {
        if (_entries.Count >= MaxNumberOfMessages) _entries.Dequeue();
        _entries.Enqueue(chatEntry);
        hudMailbox.Post(new ChatEntryAddedNotice());
    }
}
