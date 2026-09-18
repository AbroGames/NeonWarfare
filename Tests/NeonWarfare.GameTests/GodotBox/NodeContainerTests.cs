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
}
