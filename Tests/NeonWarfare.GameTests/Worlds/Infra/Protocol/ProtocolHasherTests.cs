using GdUnit4;
using MessagePack;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Protocol;

[TestSuite]
public class ProtocolHasherTests
{
    // Every run is a new process, so an input that differs between processes changes the recorded hash.
    // A deliberate change of the hash input changes it too, and then the constant is recorded anew.
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_FixedInput_GivesTheRecordedHash()
    {
        var mapping = new TypesMappingService();
        mapping.SetTypes([typeof(FixedMessage), typeof(FixedModel)]);
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, ["scene res://Fixed.tscn", "type Fixed.Node"]))
            .IsEqual(new ProtocolHash(7609314544799570749UL));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreType_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types.Append(typeof(ExtraMessage)).ToList(), Kinds))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreKind_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Append("scene res://Extra.tscn").ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // The kind id is its index in the catalog
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SwappedKinds_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Reverse().ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // A node type that stops being a kind (it became abstract, say) is still in the type list: only the kinds see it
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_TypeKindRemovedWithTheSameTypes_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(ProtocolCodecs.CreateReplicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Where(kind => !kind.StartsWith("type ")).ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    private static readonly string[] Kinds =
    [
        "scene res://Safe.tscn", "scene res://Battle.tscn",
        "type NeonWarfare.Scenes.Worlds.Features.Players.OnlinePlayersStorage",
    ];

    // Stands for a message type added in a newer build
    private class ExtraMessage;

    [MessagePackObject]
    public record FixedMessage([property: Key(0)] string Text, [property: Key(1)] int Count);

    public class FixedModel
    {
        [Replicated] public string Name = "";
        [Replicated] public int Score;
    }
}
