using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.GameTests.Worlds.Infra.Protocol;
using NeonWarfare.GameTests.Worlds.Infra.Server.Replication;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.NewWorld;
using NeonWarfare.Scenes.Worlds.Features.Options;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using NeonWarfare.Scenes.Worlds.Infra.Protocol;
using NeonWarfare.Scenes.Worlds.Infra.Server.Saves;
using NeonWarfare.Scenes.Worlds.Infra.Server.Tick;
using NeonWarfare.Scenes.Worlds.Infra.Tick;
using NeonWarfare.Scenes.Worlds.Ports;
using NeonWarfare.Scripts;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Server.Saves;

// A dedicated server through the real composition root, with the disk recorded. Its root is in the tree: a despawned
// node leaves the registry on TreeExiting
[TestSuite]
public class SaveServiceTests
{
    private static readonly SaveFileName OldFile = new("old");
    private static readonly SaveFileName NewFile = new("new");
    private const int AutoSaveIntervalMinutes = 1;

    private static readonly int IntervalTicks =
        TickRate.ToTicks(TimeSpan.FromMinutes(AutoSaveIntervalMinutes).TotalSeconds);

    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private ServiceProvider _server = null!;
    private RecordingSaveFiles _files = null!;
    private SaveService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _scenes = TestWorldScenes.Create();
        _files = new RecordingSaveFiles();
        Start(AutoSaveIntervalMinutes);
    }

    [AfterTest]
    public void TearDown()
    {
        Stop();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_InvalidName_Throws_NothingIsWritten()
    {
        AssertThrown(() => _service.RequestSave(new SaveFileName("../x"), () => { }, error => throw error))
            .IsInstanceOf<ArgumentException>();

        Tick();

        AssertThat(_files.Files).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WorldSaving_InvalidName_Throws()
    {
        AssertThrown(() => new WorldSaving(new SaveFileName("a/b"), autoSaveIntervalMinutes: 0))
            .IsInstanceOf<ArgumentException>();
    }

    // The save is the state at the end of the tick, as the joining peers get it; one save serves every request of it
    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_WrittenToTheNewFileAtTheEndOfTheTick_WhichBecomesTheSaveFile()
    {
        Tick();
        int written = 0;
        _service.RequestSave(NewFile, () => written++, error => throw error);
        _service.RequestSave(NewFile, () => written++, error => throw error);
        byte[] atTheRequest = Writer().Write();

        var counter = _server.GetRequiredService<EntitySpawner>().SpawnOnRoot<CounterNode>(node => node.Value = 1);
        counter.Value = 2;
        AssertThat(_files.Files).IsEmpty();
        Tick();

        AssertThat(written).IsEqual(2);
        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(NewFile, NewFile);
        AssertThat(_files.Files[0].Data).IsEqual(Writer().Write()).IsNotEqual(atTheRequest);
        AssertThat(_files.Files[1].Data).IsSame(_files.Files[0].Data);
        AssertThat(_service.SaveFileName).IsEqual(NewFile);
        _files.Files.Clear();

        Tick();

        AssertThat(_files.Files).IsEmpty();
        _service.SaveOnExit();
        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(NewFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_ACallbackThrows_TheOthersStillRun()
    {
        Tick();
        int written = 0;
        _service.RequestSave(NewFile, () => throw new InvalidOperationException("callback"), error => throw error);
        _service.RequestSave(NewFile, () => written++, error => throw error);

        Tick();

        AssertThat(written).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_DiskFails_GoesToFailed_TheSaveFileStays()
    {
        Tick();
        _files.Failing = true;
        List<Exception> failed = [];
        _service.RequestSave(NewFile, () => throw new InvalidOperationException("written"),
            _ => throw new InvalidOperationException("failed"));
        _service.RequestSave(NewFile, () => throw new InvalidOperationException("written"), failed.Add);

        Tick();

        AssertThat(failed).HasSize(1);
        AssertThat(_service.SaveFileName).IsEqual(OldFile);
    }

    // A failed delta has reset the baseline of a saved entity, so the writer refuses the save
    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_WriterFails_GoesToFailed_NothingIsWritten()
    {
        Tick();
        _server.GetRequiredService<EntitySpawner>().SpawnOnRoot<UnmappedPartNode>();
        List<Exception> failed = [];
        _service.RequestSave(NewFile, () => throw new InvalidOperationException("written"),
            _ => throw new InvalidOperationException("failed"));
        _service.RequestSave(NewFile, () => throw new InvalidOperationException("written"), failed.Add);

        Tick();

        AssertThat(failed).HasSize(1);
        AssertThat(_files.Files).IsEmpty();
        AssertThat(_service.SaveFileName).IsEqual(OldFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveOnExit_AfterATick_WritesTheSaveFile()
    {
        Tick();

        _service.SaveOnExit();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile);
        AssertThat(_files.Files[0].Data).IsEqual(Writer().Write());
    }

    // The writer would throw: every entity is still spawned since the last send
    [TestCase]
    [RequireGodotRuntime]
    public void SaveOnExit_BeforeTheFirstTick_WritesNothing()
    {
        _service.SaveOnExit();

        AssertThat(_files.Files).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SaveOnExit_DiskFails_DoesNotThrow()
    {
        Tick();
        _files.Failing = true;

        _service.SaveOnExit();

        AssertThat(_files.Files).IsEmpty();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_AutoSavesToTheSaveFileEveryInterval()
    {
        Ticks(IntervalTicks);
        AssertThat(_files.Files).IsEmpty();

        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile);
        AssertThat(_files.Files[0].Data).IsEqual(Writer().Write());
        Ticks(IntervalTicks - 1);
        AssertThat(_files.Files).HasSize(1);
        Tick();
        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile, OldFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_AutoSaveOff_NeverAutoSaves()
    {
        Restart(autoSaveIntervalMinutes: 0);

        Ticks(IntervalTicks * 2 + 1);

        AssertThat(_files.Files).IsEmpty();
    }

    // SaveLoader restores the tick before the first one, and the load is as fresh as a save
    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_AfterALoad_AutoSavesAnIntervalAfterTheFirstTick()
    {
        _server.GetRequiredService<ServerTickClock>().Restore(IntervalTicks * 10L);

        Ticks(IntervalTicks);
        AssertThat(_files.Files).IsEmpty();

        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_SaveAs_MovesTheAutoSaveToTheNewFile_AnIntervalLater()
    {
        Ticks(IntervalTicks / 2);
        _service.RequestSave(NewFile, () => { }, error => throw error);
        Tick();
        _files.Files.Clear();
        Ticks(IntervalTicks - 1);
        AssertThat(_files.Files).IsEmpty();

        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(NewFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_SaveAsInTheAutoSaveTick_TheNewFileIsTheSaveFile()
    {
        Ticks(IntervalTicks);
        _service.RequestSave(NewFile, () => { }, error => throw error);

        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile, NewFile);
        AssertThat(_service.SaveFileName).IsEqual(NewFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void WriteRequested_AutoSaveFails_TriedAgainAnIntervalLater()
    {
        Ticks(IntervalTicks);
        _files.Failing = true;
        Tick();
        _files.Failing = false;
        Ticks(IntervalTicks - 1);
        AssertThat(_files.Files).IsEmpty();

        Tick();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(OldFile);
    }

    private void Start(int autoSaveIntervalMinutes)
    {
        var catalog = new EntityCatalog(_scenes.GetScenesList(),
            [..NetMessageCodecTests.CreateMapping().Types, typeof(CounterNode), typeof(UnmappedPartNode)]);
        _root = new Node();
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(_root);
        var connection = new RecordingClientsConnection();
        _server = new WorldServicesBuilder().Build(
            TestWorldSetups.Dedicated(saveFiles: _files, saveFileName: OldFile,
                autoSaveIntervalMinutes: autoSaveIntervalMinutes),
            new WorldDependencies(new ManualTimeProvider(0),
                new NetMessageCodec(NetMessageCodecTests.CreateMapping()), TestProtocol.Hash,
                NetMessageCodecTests.CreateReplicator(), new ManualFrameProvider(), _scenes, catalog,
                connection, connection),
            new WorldRoot(_root));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create(WorldOptions.GetDefault());
        _service = _server.GetRequiredService<SaveService>();
    }

    private void Stop()
    {
        _server.Dispose();
        _root.Free();
    }

    private void Restart(int autoSaveIntervalMinutes)
    {
        Stop();
        Start(autoSaveIntervalMinutes);
    }

    private void Tick() => _server.GetRequiredService<ServerTickLoop>().RunTick();

    private void Ticks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Tick();
        }
    }

    private SaveWriter Writer() => _server.GetRequiredService<SaveWriter>();
}
