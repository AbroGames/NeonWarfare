using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;

namespace NeonWarfare.Scenes.Worlds.Features.Saves;

/// <summary>
/// "Save as": the World is saved to <see cref="FileName"/>, which becomes its save file. Admin only.
/// </summary>
[MessagePackObject]
public record SaveCommand(
    [property: Key(0)] string FileName) : Command;
