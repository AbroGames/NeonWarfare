using Godot;
using System;

public partial interface INodeContainer <TStored> where TStored : Node
{
	
	protected TStored CurrentStoredNode { get; set; }

	public void ChangeStoredNode(TStored newStoredNode)
	{
		CurrentStoredNode?.QueueFree();
		CurrentStoredNode = newStoredNode;
		(this as Node)?.AddChild(newStoredNode);
	}

	public void ClearStoredNode()
	{
		CurrentStoredNode?.QueueFree();
	}
}
