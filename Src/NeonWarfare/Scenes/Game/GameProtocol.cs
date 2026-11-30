using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// The protocol objects of a session. A client writes its join before it has a World, so they belong to the
/// session, not to the World.
/// </summary>
public record GameProtocol(EntityCatalog Entities, NetMessageCodec Codec, ProtocolHash Hash, Replicator Replicator)
{
    public static GameProtocol Create(WorldPackedScenes scenes, TypesMappingService mapping)
    {
        var entities = new EntityCatalog(scenes.GetScenesList(), mapping.Types);
        var replicator = new Replicator(mapping);
        return new GameProtocol(
            entities,
            new NetMessageCodec(mapping),
            new ProtocolHasher(replicator).Compute(mapping.Types, entities.Descriptors),
            replicator);
    }
}
