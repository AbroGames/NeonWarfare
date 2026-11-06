using System;
using System.Collections.Generic;
using System.Linq;

namespace NeonWarfare.Scenes.World.Features.Saves;

/// <summary>
/// A save file name valid on every OS the game runs on, whichever one checks it: a save is moved between machines,
/// and a name comes from a remote player and becomes a path on the server.
/// </summary>
public static class SaveFileNameRule
{
    public const int MaxLength = 32;

    // Windows device names, reserved with any extension and in any case
    private static readonly HashSet<string> WindowsReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", ..Numbered("COM"), ..Numbered("LPT")],
        StringComparer.OrdinalIgnoreCase);

    // The character set already rules out the separators of every OS ('/', '\', ':') and NUL. The leading dot rules
    // out "." and ".." (Linux, macOS), a hidden file, and macOS's "._" metadata files
    public static bool IsValid(string fileName) =>
        !string.IsNullOrEmpty(fileName)
        && fileName.Length <= MaxLength
        && fileName[0] != '.'
        && fileName.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')
        && !WindowsReservedNames.Contains(fileName.Split('.')[0].TrimEnd(' '));

    private static IEnumerable<string> Numbered(string prefix) =>
        Enumerable.Range(0, 10).Select(number => prefix + number);
}
