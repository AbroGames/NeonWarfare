using GdUnit4;
using Godot;
using NeonWarfare.GameTests.Worlds.Fixtures;
using NeonWarfare.Scenes.Worlds;
using NeonWarfare.Scenes.Worlds.Features.Players;
using NeonWarfare.Scenes.Worlds.Infra.Entities;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.Worlds.Infra.Entities;

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
        var storage = _spawner.SpawnOnRoot<PlayersStorage>();

        AssertThat(storage.GetParent()).IsSame(_root);
        AssertThat(storage.Name.ToString()).IsEqual(nameof(PlayersStorage));
        AssertThat(_registry.GetNode(new NetId(1))).IsSame(storage);
        AssertThat(_registry.GetSingle<PlayersStorage>()).IsSame(storage);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void SpawnByType_UnderAnEntity_GoesUnderItWithTheNextId()
    {
        var parent = _spawner.SpawnOnRoot<Node>(_scenes.SafeSurface);

        var child = _spawner.Spawn<PlayersSessionStorage>(new NetId(1));

        AssertThat(child.GetParent()).IsSame(parent);
        AssertThat(_registry.TryGetNetId(child, out NetId id)).IsTrue();
        AssertThat(id).IsEqual(new NetId(2));
        AssertThat(_registry.GetSingle<PlayersSessionStorage>()).IsSame(child);
    }

    // The kind goes into spawn records and saves: it is the only way back from a node to its scene
    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_RegistersTheCatalogKind()
    {
        EntityCatalog catalog = TestWorldScenes.CreateCatalog(_scenes);

        _spawner.SpawnOnRoot<Node>(_scenes.BattleSurface);
        _spawner.SpawnOnRoot<PlayersSessionStorage>();

        AssertThat(_registry.GetKindId(new NetId(1))).IsEqual(catalog.GetKindId(_scenes.BattleSurface));
        AssertThat(_registry.GetKindId(new NetId(2))).IsEqual(catalog.GetKindId(typeof(PlayersSessionStorage)));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Despawn_TakesTheSubtreeOutOfTheTreeAndTheRegistry_FreesItLater()
    {
        // In the tree: a node leaves the registry on TreeExiting
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(_root);
        var parent = _spawner.SpawnOnRoot<PlayersStorage>();
        var child = _spawner.Spawn<PlayersSessionStorage>(new NetId(1));
        var sibling = _spawner.SpawnOnRoot<PlayersSessionStorage>();

        _spawner.Despawn(parent);

        AssertThat(parent.GetParent()).IsNull();
        AssertThat(parent.IsQueuedForDeletion()).IsTrue();
        AssertThat(_registry.GetAll<Node>()).ContainsExactly(sibling);
        AssertThat(_registry.TryGetNetId(child, out _)).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Despawn_UnregisteredOrOutsideTheTree_ThrowsAndLeavesIt()
    {
        var registered = _spawner.SpawnOnRoot<PlayersStorage>();
        var plain = new Node();
        _root.AddChild(plain);

        AssertThrown(() => _spawner.Despawn(plain)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => _spawner.Despawn(registered)).IsInstanceOf<InvalidOperationException>();

        AssertThat(plain.GetParent()).IsSame(_root);
        AssertThat(registered.GetParent()).IsSame(_root);
        AssertThat(_registry.GetAll<Node>()).ContainsExactly(registered);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_UnknownParentOrKindOutsideCatalog_ThrowsAndCreatesNothing()
    {
        PackedScene foreign = TestWorldScenes.Pack(new Node());

        AssertThrown(() => _spawner.Spawn<Node>(_scenes.SafeSurface, new NetId(42)))
            .IsInstanceOf<KeyNotFoundException>();
        AssertThrown(() => _spawner.Spawn<PlayersSessionStorage>(new NetId(42)))
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

        AssertThrown(() => _spawner.SpawnOnRoot<PlayersSessionStorage>(_scenes.SafeSurface))
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
        var parent = _spawner.SpawnOnRoot<PlayersStorage>();
        Node? parentSeen = null;
        bool byId = false, byNode = false, inAll = false;
        _registry.SpawnedEvent += (id, node) =>
        {
            parentSeen = node.GetParent();
            byId = _registry.TryGetNode(id, out Node found) && found == node;
            byNode = _registry.TryGetNetId(node, out NetId foundId) && foundId == id;
            inAll = _registry.GetAll<PlayersSessionStorage>().Contains(node);
        };

        _spawner.Spawn<PlayersSessionStorage>(new NetId(1));

        AssertThat(parentSeen).IsSame(parent);
        AssertThat(byId).IsTrue();
        AssertThat(byNode).IsTrue();
        AssertThat(inAll).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Spawn_InitPreReady_RunsOnTheSpawnedNodeBeforeTheTreeAndTheRegistry()
    {
        PlayersSessionStorage? initialized = null;
        bool outOfTree = false, unregistered = false;

        var storage = _spawner.SpawnOnRoot<PlayersSessionStorage>(node =>
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

        AssertThrown(() => _spawner.SpawnOnRoot<PlayersSessionStorage>(
                _ => throw new InvalidOperationException("init failed")))
            .IsInstanceOf<InvalidOperationException>()
            .HasMessage("init failed");

        AssertThat(Performance.GetMonitor(Performance.Monitor.ObjectOrphanNodeCount)).IsEqual(orphans);
        AssertThat(_root.GetChildCount()).IsEqual(0);
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
        AssertThat(_netIdGenerator.NextValue).IsEqual(1L);
    }
}
