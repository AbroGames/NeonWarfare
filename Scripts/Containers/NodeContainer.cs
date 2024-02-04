using Godot;

public partial class NodeContainer : Node
{
	private Node _currentStoredNode;

	public void ChangeStoredNode(Node newStoredNode)
	{
		_currentStoredNode?.QueueFree();
		_currentStoredNode = newStoredNode;
		AddChild(newStoredNode);
	}

	public void ClearStoredNode()
	{
		_currentStoredNode?.QueueFree();
		_currentStoredNode = null;
	}

	public TStored GetCurrentStoredNode<TStored>() where TStored : Node
	{
		return _currentStoredNode as TStored;
	}
}
