namespace NeonWarfare.Scenes.Worlds.Infra.Server.Saves;

/// <summary>
/// A save written by a build of another protocol: its records would read as garbage, so none is read.
/// </summary>
public class SaveVersionMismatchException(ulong saveHash, ulong expectedHash)
    : SaveFormatException($"The save was written by another version of the game: protocol hash {saveHash:X16}, "
        + $"this version has {expectedHash:X16}.")
{
    public ulong SaveHash { get; } = saveHash;

    public ulong ExpectedHash { get; } = expectedHash;
}
