using GdUnit4;
using Godot;
using GodotBox.Godot.Nodes;
using static GdUnit4.Assertions;

namespace NeonWarfare.GameTests.GodotBox;

[TestSuite]
public class NodeContainerTests
{
    [TestCase]
    [RequireGodotRuntime]
    public void ChangeStoredNode_StoresTheNodeAsItsChild()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        Node stored = new();

        container.ChangeStoredNode(stored);

        AssertThat(container.GetCurrentStoredNode<Node>()).IsSame(stored);
        AssertThat(stored.GetParent()).IsSame(container);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ChangeStoredNode_QueuesThePreviousNodeForDeletion()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        Node previous = container.ChangeStoredNode(new Node());
        Node next = new();

        container.ChangeStoredNode(next);

        AssertThat(previous.IsQueuedForDeletion()).IsTrue();
        AssertThat(next.IsQueuedForDeletion()).IsFalse();
        AssertThat(container.GetCurrentStoredNode<Node>()).IsSame(next);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void ClearStoredNode_QueuesTheNodeAndForgetsIt()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        Node stored = container.ChangeStoredNode(new Node());

        container.ClearStoredNode();

        AssertThat(stored.IsQueuedForDeletion()).IsTrue();
        AssertThat(container.GetCurrentStoredNode<Node>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void GetCurrentStoredNode_ReturnsNullForAnotherType()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        container.ChangeStoredNode(new Node());

        AssertThat(container.GetCurrentStoredNode<Node2D>()).IsNull();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ready_AdoptsTheSingleChildAddedBeforehand()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        Node child = new();
        container.AddChild(child);

        container._Ready();

        AssertThat(container.GetCurrentStoredNode<Node>()).IsSame(child);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Ready_ThrowsOnMoreThanOneChild()
    {
        NodeContainer container = AutoFree(new NodeContainer())!;
        container.AddChild(new Node());
        container.AddChild(new Node());

        AssertThrown(() => container._Ready()).IsInstanceOf<InvalidOperationException>();
    }
}
