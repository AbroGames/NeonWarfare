using GdUnit4;
using MessagePack;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scripts;
using RepliCAT;
using RepliCAT.Bits;
using RepliCAT.Codecs;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Protocol;

// What the custom wire types write, through the resolver and the replicator the production code builds
[TestSuite]
public class ProtocolCodecsTests
{
    private static readonly PlayerUid Alice = new("AliceAlice-Aaaaaaaaaa");
    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithResolver(ProtocolCodecs.Resolver);

    [TestCase]
    [RequireGodotRuntime]
    public void Formatters_WriteBareValues_AndReadThemBack()
    {
        byte[] uid = MessagePackSerializer.Serialize(Alice, Options);
        byte[] netId = MessagePackSerializer.Serialize(new NetId(300), Options);

        AssertThat(uid).IsEqual(MessagePackSerializer.Serialize(Alice.Value));
        AssertThat(netId).IsEqual(MessagePackSerializer.Serialize(300L));
        AssertThat(MessagePackSerializer.Deserialize<PlayerUid>(uid, Options)).IsEqual(Alice);
        AssertThat(MessagePackSerializer.Deserialize<NetId>(netId, Options)).IsEqual(new NetId(300));
    }

    // A nil uid must reach the join validation as an invalid uid, not fail the message
    [TestCase]
    [RequireGodotRuntime]
    public void PlayerUidFormatter_Nil_ReadsAsDefault()
    {
        byte[] nil = MessagePackSerializer.Serialize<string?>(null);

        AssertThat(MessagePackSerializer.Deserialize<PlayerUid>(nil, Options)).IsEqual(default(PlayerUid));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetIdFormatter_Negative_Throws()
    {
        byte[] negative = MessagePackSerializer.Serialize(-1L);

        AssertThrown(() => MessagePackSerializer.Deserialize<NetId>(negative, Options))
            .IsInstanceOf<MessagePackSerializationException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Codecs_RoundTrip()
    {
        Replicator replicator = ProtocolCodecs.CreateReplicator(NetMessageCodecTests.CreateMapping());
        IReplicationCodec<PlayerUid> uidCodec = Codec<PlayerUid>(replicator);
        IReplicationCodec<NetId> netIdCodec = Codec<NetId>(replicator);
        var writer = new BitWriter();

        uidCodec.Write(writer, Alice);
        netIdCodec.Write(writer, new NetId(long.MaxValue));
        netIdCodec.Write(writer, NetId.None);

        var reader = new BitReader(writer.ToArray());
        AssertThat(uidCodec.Read(ref reader)).IsEqual(Alice);
        AssertThat(netIdCodec.Read(ref reader)).IsEqual(new NetId(long.MaxValue));
        AssertThat(netIdCodec.Read(ref reader)).IsEqual(NetId.None);
        AssertThat(reader.RemainingBits).IsEqual(0);
        AssertThat(uidCodec.IsChanged(Alice, new PlayerUid(Alice.Value))).IsFalse();
        AssertThat(netIdCodec.IsChanged(new NetId(1), new NetId(2))).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetIdCodec_SmallId_TakesOneByte()
    {
        var writer = new BitWriter();

        NetIdCodec.Instance.Write(writer, new NetId(100));

        AssertThat(writer.ToArray()).HasSize(1);
    }

    // A member not set yet holds the default uid, as a string member holds null
    [TestCase]
    [RequireGodotRuntime]
    public void PlayerUidCodec_Default_RoundTrips()
    {
        var writer = new BitWriter();

        PlayerUidCodec.Instance.Write(writer, default);

        var reader = new BitReader(writer.ToArray());
        AssertThat(PlayerUidCodec.Instance.Read(ref reader)).IsEqual(default(PlayerUid));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void NetIdCodec_OutOfRange_IsRejected()
    {
        AssertThrown(() => NetIdCodec.Instance.Write(new BitWriter(), new NetId(-1)))
            .IsInstanceOf<ReplicationException>();

        var writer = new BitWriter();
        writer.WriteVarUInt(ulong.MaxValue);
        byte[] tooLarge = writer.ToArray();
        AssertThrown(() =>
            {
                var reader = new BitReader(tooLarge);
                NetIdCodec.Instance.Read(ref reader);
            })
            .IsInstanceOf<ReplicationFormatException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickTimerFormatter_WritesTheStartAndTheDuration_AndReadsThemBack()
    {
        TickTimer timer = TickTimer.Start(long.MaxValue - int.MaxValue, int.MaxValue);

        byte[] bytes = MessagePackSerializer.Serialize(timer, Options);

        AssertThat(bytes).IsEqual(MessagePackSerializer.Serialize(new[] { timer.StartTick, int.MaxValue }));
        AssertThat(MessagePackSerializer.Deserialize<TickTimer>(bytes, Options)).IsEqual(timer);
        AssertThat(MessagePackSerializer.Deserialize<TickTimer>(
            MessagePackSerializer.Serialize(default(TickTimer), Options), Options)).IsEqual(default(TickTimer));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickTimerFormatter_InvalidInput_IsRejected()
    {
        long[][] invalid =
        [
            [-1, 10], [0, -1], [0, (long) int.MaxValue + 1], [long.MaxValue, 1], [0], [0, 10, 0],
        ];

        foreach (long[] values in invalid)
        {
            byte[] bytes = MessagePackSerializer.Serialize(values);
            AssertThrown(() => MessagePackSerializer.Deserialize<TickTimer>(bytes, Options))
                .IsInstanceOf<MessagePackSerializationException>();
        }
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickTimerCodec_RoundTrips()
    {
        Replicator replicator = ProtocolCodecs.CreateReplicator(NetMessageCodecTests.CreateMapping());
        IReplicationCodec<TickTimer> codec = Codec<TickTimer>(replicator);
        TickTimer large = TickTimer.Start(long.MaxValue - int.MaxValue, int.MaxValue);
        var writer = new BitWriter();

        codec.Write(writer, large);
        codec.Write(writer, default);

        var reader = new BitReader(writer.ToArray());
        AssertThat(codec.Read(ref reader)).IsEqual(large);
        AssertThat(codec.Read(ref reader)).IsEqual(default(TickTimer));
        AssertThat(reader.RemainingBits).IsEqual(0);
        AssertThat(codec.IsChanged(TickTimer.Start(1, 5), TickTimer.Start(1, 5))).IsFalse();
        AssertThat(codec.IsChanged(TickTimer.Start(1, 5), TickTimer.Start(2, 5))).IsTrue();
        AssertThat(codec.IsChanged(TickTimer.Start(1, 5), TickTimer.Start(1, 6))).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void TickTimerCodec_OutOfRange_IsRejected()
    {
        (ulong Start, ulong Duration)[] invalid =
        [
            ((ulong) long.MaxValue + 1, 0), (0, (ulong) int.MaxValue + 1), ((ulong) long.MaxValue, 1),
        ];

        foreach ((ulong start, ulong duration) in invalid)
        {
            var writer = new BitWriter();
            writer.WriteVarUInt(start);
            writer.WriteVarUInt(duration);
            byte[] bytes = writer.ToArray();
            AssertThrown(() =>
                {
                    var reader = new BitReader(bytes);
                    TickTimerCodec.Instance.Read(ref reader);
                })
                .IsInstanceOf<ReplicationFormatException>();
        }
    }

    private static IReplicationCodec<T> Codec<T>(Replicator replicator)
    {
        AssertThat(replicator.Codecs.TryGet(out IReplicationCodec<T> codec)).IsTrue();
        return codec;
    }
}
