using GdUnit4;
using Godot;
using NeonWarfare.Scenes.World.Entities;
using NeonWarfare.Scenes.World.Entities.Surfaces;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.World.Entities;

[TestSuite]
public class EntityRegistryTests
{
    private EntityRegistry _registry = null!;

    [BeforeTest]
    public void SetUp()
    {
        _registry = new EntityRegistry();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_LooksUpBothWays_RaisesSpawnedEvent()
    {
        Node node = AutoFree(new Node())!;
        var raised = new List<(NetId, Node)>();
        _registry.SpawnedEvent += (id, registered) => raised.Add((id, registered));

        _registry.Register(new NetId(7), node);

        AssertThat(_registry.GetNode(new NetId(7))).IsSame(node);
        AssertThat(_registry.TryGetNode(new NetId(7), out Node found)).IsTrue();
        AssertThat(found).IsSame(node);
        AssertThat(_registry.TryGetNetId(node, out NetId id)).IsTrue();
        AssertThat(id).IsEqual(new NetId(7));
        AssertThat(raised).ContainsExactly((new NetId(7), node));
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Register_NoneTakenIdOrRegisteredNode_Throws()
    {
        Node first = AutoFree(new Node())!;
        Node second = AutoFree(new Node())!;
        _registry.Register(new NetId(1), first);
        int raised = 0;
        _registry.SpawnedEvent += (_, _) => raised++;

        AssertThrown(() => _registry.Register(NetId.None, second)).IsInstanceOf<ArgumentException>();
        AssertThrown(() => _registry.Register(new NetId(1), second)).IsInstanceOf<InvalidOperationException>();
        AssertThrown(() => _registry.Register(new NetId(2), first)).IsInstanceOf<InvalidOperationException>();

        AssertThat(raised).IsEqual(0);
        AssertThat(_registry.TryGetNetId(second, out _)).IsFalse();
        AssertThat(_registry.TryGetNode(new NetId(2), out _)).IsFalse();
        AssertThrown(() => _registry.GetNode(new NetId(2))).IsInstanceOf<KeyNotFoundException>();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void RemoveChild_TakesTheNodeOut()
    {
        Node parent = InTree();
        Node node = AutoFree(new Node())!;
        parent.AddChild(node);
        _registry.Register(new NetId(1), node);

        parent.RemoveChild(node);

        AssertGone(new NetId(1), node);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Free_TakesTheNodeOut()
    {
        Node parent = InTree();
        var node = new Node();
        parent.AddChild(node);
        _registry.Register(new NetId(1), node);

        node.Free();

        AssertThat(_registry.TryGetNode(new NetId(1), out _)).IsFalse();
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
    }

    // Only the descendants are registered: the signal reaches them from an ancestor the registry does not know
    [TestCase]
    [RequireGodotRuntime]
    public void FreeOfAnAncestor_TakesTheWholeSubtreeOut()
    {
        Node parent = InTree();
        var subtree = new Node();
        var child = new Node();
        var grandchild = new Node();
        parent.AddChild(subtree);
        subtree.AddChild(child);
        child.AddChild(grandchild);
        Node sibling = new();
        parent.AddChild(sibling);
        _registry.Register(new NetId(1), child);
        _registry.Register(new NetId(2), grandchild);
        _registry.Register(new NetId(3), sibling);

        subtree.Free();

        AssertThat(_registry.TryGetNode(new NetId(1), out _)).IsFalse();
        AssertThat(_registry.TryGetNode(new NetId(2), out _)).IsFalse();
        AssertThat(_registry.GetAll<Node>()).ContainsExactly(sibling);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetAll_MatchesClassAndBaseClass_OrderedByNetId()
    {
        SafeSurface safe = AutoFree(new SafeSurface())!;
        BattleSurface battle = AutoFree(new BattleSurface())!;
        Node plain = AutoFree(new Node())!;
        _registry.Register(new NetId(5), safe);
        _registry.Register(new NetId(3), battle);
        _registry.Register(new NetId(1), plain);

        AssertThat(_registry.GetAll<SafeSurface>()).ContainsExactly(safe);
        AssertThat(_registry.GetAll<Surface>()).ContainsExactly(battle, safe);
        AssertThat(_registry.GetAll<Node>()).ContainsExactly(plain, battle, safe);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetAll_FollowsChangesAfterTheFirstRequest()
    {
        Node parent = InTree();
        SafeSurface first = AutoFree(new SafeSurface())!;
        SafeSurface second = AutoFree(new SafeSurface())!;
        parent.AddChild(first);
        _registry.Register(new NetId(1), first);
        AssertThat(_registry.GetAll<Surface>()).ContainsExactly(first);

        _registry.Register(new NetId(2), second);
        AssertThat(_registry.GetAll<Surface>()).ContainsExactly(first, second);

        parent.RemoveChild(first);
        AssertThat(_registry.GetAll<Surface>()).ContainsExactly(second);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetAll_SnapshotIsSharedWhileUnchanged_AndNeverChanges()
    {
        Node parent = InTree();
        SafeSurface first = AutoFree(new SafeSurface())!;
        parent.AddChild(first);
        _registry.Register(new NetId(1), first);

        IReadOnlyList<SafeSurface> before = _registry.GetAll<SafeSurface>();
        AssertThat(_registry.GetAll<SafeSurface>()).IsSame(before);

        _registry.Register(new NetId(2), AutoFree(new SafeSurface())!);
        IReadOnlyList<SafeSurface> afterRegister = _registry.GetAll<SafeSurface>();
        parent.RemoveChild(first);

        AssertThat(before).ContainsExactly(first);
        AssertThat(afterRegister.Count).IsEqual(2);
        AssertThat(_registry.GetAll<SafeSurface>().Count).IsEqual(1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetSingle_ReturnsTheOnlyMatch_ThrowsOnNoneOrSeveral()
    {
        SafeSurface safe = AutoFree(new SafeSurface())!;
        AssertThrown(() => _registry.GetSingle<Surface>())
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one Surface, found 0.");

        _registry.Register(new NetId(1), safe);
        AssertThat(_registry.GetSingle<Surface>()).IsSame(safe);

        _registry.Register(new NetId(2), AutoFree(new BattleSurface())!);
        AssertThrown(() => _registry.GetSingle<Surface>())
            .IsInstanceOf<InvalidOperationException>()
            .StartsWithMessage("Expected exactly one Surface, found 2.");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Exists_FollowsRegistrationAndRemoval()
    {
        Node parent = InTree();
        SafeSurface safe = AutoFree(new SafeSurface())!;
        parent.AddChild(safe);
        AssertThat(_registry.Exists<Surface>()).IsFalse();

        _registry.Register(new NetId(1), safe);
        AssertThat(_registry.Exists<Surface>()).IsTrue();
        AssertThat(_registry.Exists<BattleSurface>()).IsFalse();

        parent.RemoveChild(safe);
        AssertThat(_registry.Exists<Surface>()).IsFalse();
    }

    private void AssertGone(NetId id, Node node)
    {
        AssertThat(_registry.TryGetNode(id, out _)).IsFalse();
        AssertThat(_registry.TryGetNetId(node, out _)).IsFalse();
        AssertThat(_registry.GetAll<Node>()).IsEmpty();
    }

    // TreeExiting fires only for a node inside a tree
    private static Node InTree()
    {
        Node parent = AutoFree(new Node())!;
        ((SceneTree) Engine.GetMainLoop()).Root.AddChild(parent);
        return parent;
    }
}
