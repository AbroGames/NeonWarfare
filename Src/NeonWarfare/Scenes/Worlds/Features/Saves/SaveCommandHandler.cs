using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.ServerNetwork.Commands;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

// Only an admin is shown the save controls, so a refused command gets no reply
[CommandHandler]
public class SaveCommandHandler(SaveSimulationFacade saveSimulationFacade, PlayerQuery players)
    : IPlayerCommandHandler<SaveCommand>
{
    public const int MaxFileNameLength = 32;

    // Windows device names, reserved with any extension and in any case
    private static readonly HashSet<string> WindowsReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", ..Numbered("COM"), ..Numbered("LPT")],
        StringComparer.OrdinalIgnoreCase);

    public bool Validate(string senderUid, SaveCommand command) =>
        players.Get(senderUid).IsAdmin && IsValidFileName(command.FileName);

    public void Process(string senderUid, SaveCommand command) =>
        saveSimulationFacade.Save(senderUid, command.FileName);

    /// <summary>
    /// Whether a save file name is valid on every OS the game runs on, whichever one checks it: a save is moved between
    /// machines, and a name comes from a remote player and becomes a path on the server.
    /// </summary>
    // The character set already rules out the separators of every OS ('/', '\', ':') and NUL. The leading dot rules
    // out "." and ".." (Linux, macOS), a hidden file, and macOS's "._" metadata files
    public static bool IsValidFileName(string fileName) =>
        !string.IsNullOrEmpty(fileName)
        && fileName.Length <= MaxFileNameLength
        && fileName[0] != '.'
        && fileName.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')
        && !WindowsReservedNames.Contains(fileName.Split('.')[0].TrimEnd(' '));

    private static IEnumerable<string> Numbered(string prefix) =>
        Enumerable.Range(0, 10).Select(number => prefix + number);
}
