using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scripts;

/// <summary>
/// The name of a save file, without its folder and extension.
/// The constructor does not validate, because a malformed name from a peer, a file or the command line must reach a
/// check that answers it.
/// </summary>
public readonly record struct SaveFileName(string Value)
{
    public const int MaxLength = 32;

    // The Windows device names, reserved with any extension and in any case.
    private static readonly HashSet<string> WindowsReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", ..Numbered("COM"), ..Numbered("LPT")],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Whether the name is valid on every OS the game runs on, whichever OS checks it.
    /// A save is moved between machines, and the name comes from a remote player and becomes a path on the server.
    /// <list type="bullet">
    /// <item>The character set rules out the separators of every OS (<c>/</c>, <c>\</c>, <c>:</c>) and NUL.</item>
    /// <item>The ban on a leading dot rules out <c>.</c> and <c>..</c>, hidden files and the <c>._</c> metadata
    /// files of macOS.</item>
    /// </list>
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(Value)
                           && Value.Length <= MaxLength
                           && Value[0] != '.'
                           && Value.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')
                           && !WindowsReservedNames.Contains(Value.Split('.')[0].TrimEnd(' '));

    /// <summary>For a name this process takes on trust, so that a malformed one is caught where it enters.</summary>
    /// <returns><c>false</c> and <c>default</c> when the name is invalid.</returns>
    public static bool TryParse(string value, out SaveFileName fileName)
    {
        var parsed = new SaveFileName(value);
        fileName = parsed.IsValid ? parsed : default;
        return parsed.IsValid;
    }

    public override string ToString() => Value;

    private static IEnumerable<string> Numbered(string prefix) =>
        Enumerable.Range(0, 10).Select(number => prefix + number);
}
