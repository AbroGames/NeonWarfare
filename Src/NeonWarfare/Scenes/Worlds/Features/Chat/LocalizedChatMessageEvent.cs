using System;
using System.Linq;
using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

/// <summary>
/// A server message the receiver translates itself, see Docs/Chat-and-commands.md#localized-server-messages.
/// </summary>
[MessagePackObject]
public sealed record LocalizedChatMessageEvent(
    [property: Key(0)] long SentAtUnixSeconds,
    [property: Key(1)] string Key,
    [property: Key(2)] string[] Args) : Event
{
    /// <summary>Compares <c>Args</c> by content, not by reference as the generated equality would.</summary>
    public bool Equals(LocalizedChatMessageEvent other) =>
        other is not null
        && SentAtUnixSeconds == other.SentAtUnixSeconds
        && Key == other.Key
        && Args.SequenceEqual(other.Args);

    public override int GetHashCode() => HashCode.Combine(SentAtUnixSeconds, Key, Args.Length);
}
