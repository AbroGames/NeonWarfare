using System.Linq;
using System.Security.Cryptography;

namespace NeonWarfare.Scripts;

/// <summary>
/// The identity of a player across sessions and saves: two groups of Latin letters joined by a dash.
/// The constructor does not validate, because a malformed uid from a peer must reach the join validation.
/// </summary>
public readonly record struct PlayerUid(string Value)
{
    private const int GroupLength = 10;
    private const char Separator = '-';
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public bool IsValid => Value is { Length: GroupLength * 2 + 1 }
                           && Value[GroupLength] == Separator
                           && Value.Where((_, i) => i != GroupLength).All(char.IsAsciiLetter);

    public static PlayerUid Generate() =>
        new(RandomNumberGenerator.GetString(Alphabet, GroupLength)
            + Separator
            + RandomNumberGenerator.GetString(Alphabet, GroupLength));

    public override string ToString() => Value;
}
