using System;
using System.Linq;
using MessagePack;

namespace NeonWarfare.Scenes.Worlds.Features.Chat;

/// <summary>
/// A text the receiver translates itself, see Docs/Chat-and-commands.md#localized-server-messages.
/// </summary>
[MessagePackObject]
public sealed record LocalizedText(
    [property: Key(0)] string Key,
    [property: Key(1)] string[] Args)
{
    /// <summary>Compares <c>Args</c> by content, not by reference as the generated equality would.</summary>
    public bool Equals(LocalizedText other)
    {
        return other is not null
               && Key == other.Key
               && Args.SequenceEqual(other.Args);
    }

    public override int GetHashCode() => HashCode.Combine(Key, Args.Length);
}
