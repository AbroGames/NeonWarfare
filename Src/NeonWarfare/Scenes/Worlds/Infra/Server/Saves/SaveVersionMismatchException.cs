using Humanizer;

namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// A save written by a build of another protocol: its records would read as garbage, so none is read.
/// </summary>
public class SaveVersionMismatchException(ulong saveHash, ulong expectedHash)
    : SaveFormatException(MismatchError.FormatWith(saveHash, expectedHash))
{
    private const string MismatchError =
        "The save was written by another version of the game: protocol hash {0:X16}, this version has {1:X16}.";

    public ulong SaveHash { get; } = saveHash;

    public ulong ExpectedHash { get; } = expectedHash;
}
