using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

/// <summary>
/// "Save as": saves the World to <see cref="FileName"/>, which becomes its save file. Only an admin may send it.
/// </summary>
[MessagePackObject]
public record SaveCommand(
    [property: Key(0)] string FileName) : Command;
