using System;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// The protocol objects of one session: one catalog for World and the codec, so the protocol hash covers every entity
/// kind World may spawn.
/// </summary>
public static class GameProtocol
{
    private const string NotReadyError =
        "WorldPackedScenes fills its scene list in _Ready: a catalog built before it has no scene kinds.";

    public static (EntityCatalog Entities, NetMessageCodec Codec) Create(
        WorldPackedScenes scenes, TypesMappingService mapping)
    {
        if (!scenes.IsNodeReady()) throw new InvalidOperationException(NotReadyError);

        var entities = new EntityCatalog(scenes.GetScenesList(), mapping.Types);
        return (entities, new NetMessageCodec(mapping, entities.Descriptors));
    }
}
