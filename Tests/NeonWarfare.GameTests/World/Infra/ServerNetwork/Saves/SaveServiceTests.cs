using GdUnit4;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.GameTests.World.Infra.Protocol;
using NeonWarfare.GameTests.World.Infra.Replication;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.NewWorld;
using NeonWarfare.Scenes.World.Infra.Composition;
using NeonWarfare.Scenes.World.Infra.Entities;
using NeonWarfare.Scenes.World.Infra.Protocol;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Saves;
using NeonWarfare.Scenes.World.Infra.ServerNetwork.Tick;
using RepliCAT;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.ServerNetwork.Saves;

// A dedicated server through the real composition root, with the disk recorded. Its root is in the tree: a despawned
// node leaves the registry on TreeExiting
[TestSuite]
public class SaveServiceTests
{
    private const string OldFile = "old";
    private const string NewFile = "new";

    private WorldPackedScenes _scenes = null!;
    private Node _root = null!;
    private ServiceProvider _server = null!;
    private RecordingSaveFiles _files = null!;
    private SaveService _service = null!;

    [BeforeTest]
    public void SetUp()
    {
        _scenes = TestWorldScenes.Create();
        var catalog = new EntityCatalog(_scenes.GetScenesList(),
            [..NetMessageCodecTests.CreateMapping().Types, typeof(UnmappedPartNode)]);
        _root = new Node();
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(_root);
        _files = new RecordingSaveFiles();
        var connection = new RecordingClientsConnection();
        _server = new WorldServicesBuilder().Build(
            WorldLayer.Dedicated,
            new WorldDependencies(new ManualTimeProvider(0),
                new NetMessageCodec(NetMessageCodecTests.CreateMapping(), []),
                new Replicator(NetMessageCodecTests.CreateMapping()), new ManualFrameProvider(), _scenes, catalog,
                connection, connection, _files, LocalPlayer: null,
                TestWorldDependencies.Admin(WorldLayer.Dedicated),
                TestWorldDependencies.DedicatedServerOwner(WorldLayer.Dedicated)),
            new WorldRoot(_root));
        _server.GetRequiredService<NewWorldSimulationFacade>().Create();
        _service = _server.GetRequiredService<SaveService>();
        _service.Init(OldFile);
    }

    [AfterTest]
    public void TearDown()
    {
        _server.Dispose();
        _root.Free();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_WrittenToTheNewFileAtTheEndOfTheTick_WhichBecomesTheSaveFile()
    {
        Tick();
        int written = 0;
        _service.RequestSave(NewFile, () => written++, error => throw error);

        AssertThat(_files.Files).IsEmpty();
        Tick();

        AssertThat(written).IsEqual(1);
        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(NewFile);
        AssertThat(_files.Files[0].Data).IsEqual(Writer().Write());
        AssertThat(_service.SaveFileName).IsEqual(NewFile);

        _service.SaveOnExit();

        AssertThat(_files.Files.Select(file => file.FileName)).ContainsExactly(NewFile, NewFile);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RequestSave_DiskFails_GoesToFailed_TheSaveFileStays()
    {
        Tick();
        _files.Failing = true;
        List<Exception> failed = [];
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

    [TestCase]
    [RequireGodotRuntime]
    public void SaveOnExit_AutoSaveDisabled_WritesNothing()
    {
        Tick();
        _files.AutoSaveEnabled = false;

        _service.SaveOnExit();

        AssertThat(_files.Files).IsEmpty();
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
    public void Init_Twice_Throws()
    {
        AssertThrown(() => _service.Init(NewFile)).IsInstanceOf<InvalidOperationException>();
        AssertThat(_service.SaveFileName).IsEqual(OldFile);
    }

    private void Tick() => _server.GetRequiredService<ServerTickLoop>().RunTick();

    private SaveWriter Writer() => _server.GetRequiredService<SaveWriter>();
}
