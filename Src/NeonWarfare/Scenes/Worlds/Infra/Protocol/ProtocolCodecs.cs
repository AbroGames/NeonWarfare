using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using RepliCAT;
using RepliCAT.Codecs;

namespace NeonWarfare.Scenes.Worlds.Infra.Protocol;

/// <summary>
/// The one list of the custom wire types: every MessagePack formatter and RepliCAT codec of the game.
/// The protocol hash sees only their names, so a change of what one of them writes does not change the hash.
/// </summary>
public static class ProtocolCodecs
{
    public static readonly IFormatterResolver Resolver = CompositeResolver.Create(
        new IMessagePackFormatter[]
        {
            ColorFormatter.Instance, PlayerUidFormatter.Instance, NetIdFormatter.Instance, TickTimerFormatter.Instance,
        },
        new IFormatterResolver[] { StandardResolver.Instance });

    public static Replicator CreateReplicator(ITypeIdMapping mapping)
    {
        var codecs = new ReplicationCodecs();
        codecs.Register(PlayerUidCodec.Instance);
        codecs.Register(NetIdCodec.Instance);
        codecs.Register(TickTimerCodec.Instance);
        return new Replicator(mapping, codecs);
    }
}
