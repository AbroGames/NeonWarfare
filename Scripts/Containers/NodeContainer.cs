using Godot;

public partial class NodeContainer<TStored> : Node where TStored : Node
{
	public TStored CurrentStoredNode { get; private set; }

	public void ChangeStoredNode(TStored newStoredNode)
	{
		CurrentStoredNode?.QueueFree();
		CurrentStoredNode = newStoredNode;
		AddChild(newStoredNode);
	}

	public void ClearStoredNode()
	{
		CurrentStoredNode?.QueueFree();
		CurrentStoredNode = null;
	}
}
