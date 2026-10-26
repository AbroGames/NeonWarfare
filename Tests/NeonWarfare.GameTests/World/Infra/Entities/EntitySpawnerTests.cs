using GdUnit4;
using Godot;
using NeonWarfare.GameTests.World.Fixtures;
using NeonWarfare.Scenes.World;
using NeonWarfare.Scenes.World.Features.Storages;
using NeonWarfare.Scenes.World.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Infra.Entities;

[TestSuite]
public class EntitySpawnerTests
{
    private Node _root = null!;
    private WorldPackedScenes _scenes = null!;
    private EntityRegistry _registry = null!;
    private NetIdGenerator _netIdGenerator = null!;
    private EntitySpawner _spawner = null!;

    [BeforeTest]
    public void SetUp()
    {
        _root = new Node();
        _scenes = TestWorldScenes.Create();
        _registry = new EntityRegistry();
        _netIdGenerator = new NetIdGenerator();
        _spawner = new EntitySpawner(
            _netIdGenerator, _registry, new WorldRoot(_root), TestWorldScenes.CreateCatalog(_scenes));
    }

    [AfterTest]
    public void TearDown()
    {
        _root.Free();
        _scenes.Free();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_NoParent_GoesUnderTheRootWithTheFirstId()
    {
        var entity = _spawner.SpawnOnRoot<Node>(_scenes.SafeSurface);

        AssertThat(entity.GetParent()).IsSame(_root);
        AssertThat(_registry.GetNode(new NetId(1))).IsSame(entity);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_UnderAnEntity_GoesUnderIt()
    {
        var parent = _spawner.SpawnOnRoot<Node>(_scenes.SafeSurface);

        var child = _spawner.Spawn<Node>(_scenes.BattleSurface, new NetId(1));

        AssertThat(child.GetParent()).IsSame(parent);
        AssertThat(_registry.TryGetNetId(child, out NetId id)).IsTrue();
        AssertThat(id).IsEqual(new NetId(2));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SpawnByType_NoParent_GoesUnderTheRootWithTheFirstIdAndIsFound()
    {
        var storage = _spawner.SpawnOnRoot<PersistenceStorage>();

        AssertThat(storage.GetParent()).IsSame(_root);
        AssertThat(storage.Name.ToString()).IsEqual(nameof(PersistenceStorage));
        AssertThat(_registry.GetNode(new NetId(1))).IsSame(storage);
        AssertThat(_registry.GetSingle<PersistenceStorage>()).IsSame(storage);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SpawnByType_UnderAnEntity_GoesUnderItWithTheNextId()
    {
        var parent = _spawner.SpawnOnRoot<Node>(_scenes.SafeSurface);

        var child = _spawner.Spawn<SessionStorage>(new NetId(1));

        AssertThat(child.GetParent()).IsSame(parent);
        AssertThat(_registry.TryGetNetId(child, out NetId id)).IsTrue();
        AssertThat(id).IsEqual(new NetId(2));
        AssertThat(_registry.GetSingle<SessionStorage>()).IsSame(child);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_UnknownParentOrKindOutsideCatalog_ThrowsAndCreatesNothing()
    {
        PackedScene foreign = TestWorldScenes.Pack(new Node());

        AssertThrown(() => _spawner.Spawn<Node>(_scenes.SafeSurface, new NetId(42)))
            .IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => _spawner.Spawn<SessionStorage>(new NetId(42)))
            .IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => _spawner.SpawnOnRoot<Node>(foreign)).IsInstanceOf<ArgumentException>();
        // An engine class: not in the game's type mapping
        AssertThrown(() => _spawner.SpawnOnRoot<Node2D>()).IsInstanceOf<ArgumentException>();

        AssertThat(_root.GetChildCount()).IsEqual(0);
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
        AssertThat(_netIdGenerator.NextValue).IsEqual(1L);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_SceneRootOfAnotherType_ThrowsAndLeavesNoNode()
    {
        double orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        AssertThrown(() => _spawner.SpawnOnRoot<SessionStorage>(_scenes.SafeSurface))
            .IsInstanceOf<ArgumentException>()
            .StartsWithMessage("The root of");

        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphans);
        AssertThat(_root.GetChildCount()).IsEqual(0);
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
        AssertThat(_netIdGenerator.NextValue).IsEqual(1L);
    }

    // Subscribers look the entity up and reach its parent: both must be in place when the event is raised
    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_SpawnedEvent_SeesTheNodeInTheTreeAndInTheRegistry()
    {
        var parent = _spawner.SpawnOnRoot<PersistenceStorage>();
        Node? parentSeen = null;
        bool byId = false, byNode = false, inAll = false;
        _registry.SpawnedEvent += (id, node) =>
        {
            parentSeen = node.GetParent();
            byId = _registry.TryGetNode(id, out Node found) && found == node;
            byNode = _registry.TryGetNetId(node, out NetId foundId) && foundId == id;
            inAll = _registry.GetAll<SessionStorage>().Contains(node);
        };

        _spawner.Spawn<SessionStorage>(new NetId(1));

        AssertThat(parentSeen).IsSame(parent);
        AssertThat(byId).IsTrue();
        AssertThat(byNode).IsTrue();
        AssertThat(inAll).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_InitPreReady_RunsOnTheSpawnedNodeBeforeTheTreeAndTheRegistry()
    {
        SessionStorage? initialized = null;
        bool outOfTree = false, unregistered = false;

        var storage = _spawner.SpawnOnRoot<SessionStorage>(node =>
        {
            initialized = node;
            outOfTree = node.GetParent() == null;
            unregistered = !_registry.TryGetNetId(node, out _);
        });

        AssertThat(initialized).IsSame(storage);
        AssertThat(outOfTree).IsTrue();
        AssertThat(unregistered).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_InitPreReadyThrows_LeavesNoNode()
    {
        double orphans = Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount);

        AssertThrown(() => _spawner.SpawnOnRoot<SessionStorage>(
                _ => throw new InvalidOperationException("init failed")))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage("init failed");

        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphans);
        AssertThat(_root.GetChildCount()).IsEqual(0);
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
        AssertThat(_netIdGenerator.NextValue).IsEqual(1L);
    }
}
