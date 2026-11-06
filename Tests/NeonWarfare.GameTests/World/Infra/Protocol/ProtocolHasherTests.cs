using GdUnit4;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.Protocol;

[TestSuite]
public class ProtocolHasherTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SameMapping_GivesSameHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Scenes))
            .IsEqual(hasher.Compute(mapping.Types.ToList(), Scenes.ToList()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreType_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types.Append(typeof(ExtraMessage)).ToList(), Scenes))
            .IsNotEqual(hasher.Compute(mapping.Types, Scenes));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreScene_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Scenes.Append("res://Extra.tscn").ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Scenes));
    }

    // The scene id is its index in the catalog
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SwappedScenes_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types, Scenes.Reverse().ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types, Scenes));
    }

    private static readonly string[] Scenes = ["res://Safe.tscn", "res://Battle.tscn"];

    // Stands for a message type added in a newer build
    private class ExtraMessage;
}
