using System;
using System.Collections.Generic;
using System.Linq;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Composition;
using NeonWarfare.Scenes.Worlds.Infra.Server.Commands;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

/// <summary>A refused command gets no reply, because only an admin is shown the save controls.</summary>
[CommandHandler]
public class SaveCommandHandler(SaveSimulationFacade saveSimulationFacade, PlayerQuery players)
    : IPlayerCommandHandler<SaveCommand>
{
    public const int MaxFileNameLength = 32;

    // The Windows device names, reserved with any extension and in any case.
    private static readonly HashSet<string> WindowsReservedNames = new(
        ["CON", "PRN", "AUX", "NUL", ..Numbered("COM"), ..Numbered("LPT")],
        StringComparer.OrdinalIgnoreCase);

    public bool Validate(PlayerUid senderUid, SaveCommand command) =>
        players.Get(senderUid).IsAdmin && IsValidFileName(command.FileName);

    public void Process(PlayerUid senderUid, SaveCommand command) =>
        saveSimulationFacade.Save(senderUid, command.FileName);

    /// <summary>
    /// Whether a save file name is valid on every OS the game runs on, whichever OS checks it.
    /// A save is moved between machines, and the name comes from a remote player and becomes a path on the server.
    /// <list type="bullet">
    /// <item>The character set rules out the separators of every OS (<c>/</c>, <c>\</c>, <c>:</c>) and NUL.</item>
    /// <item>The ban on a leading dot rules out <c>.</c> and <c>..</c>, hidden files and the <c>._</c> metadata
    /// files of macOS.</item>
    /// </list>
    /// </summary>
    public static bool IsValidFileName(string fileName) =>
        !string.IsNullOrEmpty(fileName)
        && fileName.Length <= MaxFileNameLength
        && fileName[0] != '.'
        && fileName.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_' or '.')
        && !WindowsReservedNames.Contains(fileName.Split('.')[0].TrimEnd(' '));

    private static IEnumerable<string> Numbered(string prefix) =>
        Enumerable.Range(0, 10).Select(number => prefix + number);
}
