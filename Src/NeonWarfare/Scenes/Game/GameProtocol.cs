using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using RepliCAT;

namespace NeonWarfare.Scenes.Game;

/// <summary>
/// The protocol objects of a session. A client writes its join before it has a World, so they belong to the
/// session, not to the World.
/// </summary>
public record GameProtocol(EntityCatalog Entities, NetMessageCodec Codec, Replicator Replicator)
{
    public static GameProtocol Create(WorldPackedScenes scenes)
    {
        var entities = new EntityCatalog(scenes.GetScenesList(), Services.TypesMapping.Types);
        return new GameProtocol(
            entities,
            new NetMessageCodec(Services.TypesMapping, entities.Descriptors),
            new Replicator(Services.TypesMapping));
    }
}
