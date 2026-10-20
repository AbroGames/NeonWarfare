using GdUnit4;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.ServerNetwork;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.ServerNetwork;

[TestSuite]
public class NetIdGeneratorTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void Next_StartsAtOne_GrowsByOne()
    {
        var generator = new NetIdGenerator();

        AssertThat(generator.NextValue).IsEqual(1L);
        AssertThat(new[] { generator.Next(), generator.Next(), generator.Next() })
            .ContainsExactly(new NetId(1), new NetId(2), new NetId(3));
        AssertThat(generator.NextValue).IsEqual(4L);
    }
}
