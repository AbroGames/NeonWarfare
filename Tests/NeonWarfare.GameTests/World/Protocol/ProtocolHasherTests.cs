using GdUnit4;
using NeonWarfare.Scenes.World.Protocol;
using NeonWarfare.Scripts.GlobalServices;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Protocol;

[TestSuite]
public class ProtocolHasherTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Compute_SameMapping_GivesSameHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types)).IsEqual(hasher.Compute(mapping.Types.ToList()));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Compute_OneMoreType_ChangesHash()
    {
        TypesMappingService mapping = NetMessageCodecTests.CreateMapping();
        var hasher = new ProtocolHasher(new Replicator(mapping));

        AssertThat(hasher.Compute(mapping.Types.Append(typeof(ExtraMessage)).ToList()))
            .IsNotEqual(hasher.Compute(mapping.Types));
    }

    // Stands for a message type added in a newer build
    private class ExtraMessage;
}
