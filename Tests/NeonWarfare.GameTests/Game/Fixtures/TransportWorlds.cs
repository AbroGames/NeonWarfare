using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Game.Fixtures;

/// <summary>
/// Builds a World over a transport as <c>Game</c> does: the World exists before its transport, which is built over
/// it, and is initialized with that transport only then.
/// </summary>
public class TransportWorlds
{
    public const long Now = 1_700_000_000;

    public NetMessageCodec Codec { get; } = new(NetMessageCodecTests.CreateMapping());

    public ManualTimeProvider Time { get; } = new(Now);

    public World Init(
        World world, WorldSetup setup, IClientsConnection clients, IServerConnection server,
        WorldOrigin? origin = null)
    {
        WorldPackedScenes scenes = AutoFree(TestWorldScenes.Create())!;
        var dependencies = new WorldDependencies(
            Time, Codec, TestProtocol.Hash, NetMessageCodecTests.CreateReplicator(),
            new ManualFrameProvider(), scenes, TestWorldScenes.CreateCatalog(scenes), clients, server);
        return world.InitPreReady(setup, dependencies, origin ?? new WorldOrigin.NewWorld());
    }

    // Outside the tree the engine never ticks the World
    public static void Tick(World world) =>
        world.GetChildren().OfType<WorldTickNode>().Single()._PhysicsProcess(0);
}
