using GdUnit4;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Protocol;

[TestSuite]
public class ProtocolHasherTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SameMapping_GivesSameHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds))
            .IsEqual(hasher.Compute(mapping.Types.ToList(), Kinds.ToList()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreType_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types.Append(typeof(ExtraMessage)).ToList(), Kinds))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreKind_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Append("scene res://Extra.tscn").ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // The kind id is its index in the catalog
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SwappedKinds_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Reverse().ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    // A node type that stops being a kind (it became abstract, say) is still in the type list: only the kinds see it
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_TypeKindRemovedWithTheSameTypes_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Kinds.Where(kind => !kind.StartsWith("type ")).ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Kinds));
    }

    private static readonly string[] Kinds =
    [
        "scene res://Safe.tscn", "scene res://Battle.tscn",
        "type NeonWarfare.Scenes.Worlds.Features.Players.PlayersSessionStorage",
    ];

    // Stands for a message type added in a newer build
    private class ExtraMessage;
}
