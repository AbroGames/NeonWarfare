using System;
using System.Linq;
using MessagePack;
using NeonWarfare.Scenes.World.Infra.Protocol;

namespace NeonWarfare.Scenes.World.Features.Chat;

/// <summary>
/// A server message the receiver translates itself: Tr on the server would use the server's language.
/// The arguments are already strings, substituted into the translation by position ({0}, {1}, …).
/// </summary>
[MessagePackObject]
public sealed record LocalizedChatMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Key,
    [property: Key(2)] string[] Args) : Event
{
    // The generated equality compares the array by reference
    public bool Equals(LocalizedChatMessageEvent other) =>
        other is not null
        && SentAtUnixSeconds == other.SentAtUnixSeconds
        && Key == other.Key
        && Args.SequenceEqual(other.Args);

    public override int GetHashCode() => HashCode.Combine(SentAtUnixSeconds, Key, Args.Length);
}
