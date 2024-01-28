using Godot;
using System;

public partial class WorldContainer : Node2D, INodeContainer<Node2D>
{
	public Node2D CurrentStoredNode { get; set; }
	
	public void ChangeStoredNode(Node2D newStoredNode)
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

