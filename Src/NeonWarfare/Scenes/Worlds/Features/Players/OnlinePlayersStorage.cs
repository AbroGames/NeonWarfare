using Godot;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using RepliCAT;

namespace NeonWarfare.Scenes.Worlds.Features.Players;

/// <summary>
/// The entity holding <see cref="OnlinePlayersModel"/>, one per world, found through
/// <see cref="OnlinePlayersStorageQuery"/>.
/// After a load nobody is online until the players join again, see Docs/Data-and-saves.md#models-and-storages.
/// </summary>
[NotSaved]
public partial class OnlinePlayersStorage : Node
{
    [Replicated] public readonly OnlinePlayersModel Model = new();
}
