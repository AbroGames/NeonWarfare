using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Game.Fixtures;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Game;
using NeonWarfare.Scenes.Game.Transport;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game;

[TestSuite]
public class WorldAssemblerTests
{
    private const string SaveFileName = "save";

    private static readonly byte[] BrokenSave = [1, 2, 3, 4];

    private WorldAssembler _assembler = null!;
    private NetMessageCodec _codec = null!;
    private RecordingLocalPlayerOwner _owner = null!;

    [BeforeTest]
    public void SetUp()
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        _codec = new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []);
        var protocol = new GameProtocol(
            TestWorldScenes.CreateCatalog(scenes), _codec, new Replicator(NetMessageCodecTests.CreateMapping()));
        _assembler = new WorldAssembler(protocol, scenes);
        _owner = new RecordingLocalPlayerOwner();
    }

    // Single player
    [TestCase]
    [RequireGodotRuntime]
    public void Host_WithoutANetwork_TheHostJoinsInTheNextTick()
    {
        World world = AutoFree(_assembler.Host(
            TestWorldSetups.Host(localPlayerOwner: _owner), new WorldOrigin.NewWorld(SaveFileName), null))!;

        TransportWorlds.Tick(world);

        AssertThat(_owner.JoinedCount).IsEqual(1);
        AssertThat(world.Get<LocalPlayerPresentation>().Player.Uid).IsEqual(TestWorldSetups.LocalPlayerUid);
    }

    // The network outlives the World: left subscribed, it would hand the next World's peers to a freed one
    [TestCase]
    [RequireGodotRuntime]
    public void Dedicated_BrokenSave_FreesTheWorldAndDetachesFromTheNetwork()
    {
        var network = new FakeNetwork();
        double orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        AssertThrown(() => _assembler.Dedicated(
                TestWorldSetups.Dedicated(), new WorldOrigin.FromSave(BrokenSave, SaveFileName), network))
            .IsInstanceOf<SaveFormatException>();

        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphans);
        AssertThat(network.HasListeners).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RemoteClient_BrokenSnapshot_FreesTheWorld()
    {
        var transport = new ClientTransport(
            new FakeNetwork(), _codec, TestWorldSetups.LocalPlayer(), _owner, _ => { });
        double orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        NetMessageCodecTests.AssertRejected(() => _assembler.RemoteClient(
            TestWorldSetups.RemoteClient(localPlayerOwner: _owner), new byte[] { 1 }, transport));

        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphans);
    }
}
