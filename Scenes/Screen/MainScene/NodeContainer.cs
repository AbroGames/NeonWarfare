using System;
using Godot;

namespace NeonWarfare.Scenes.Screen.MainScene;

public partial class NodeContainer : Node
{
	private Node _currentStoredNode;

	public override void _Ready()
	{
		if (GetChildCount() > 1) throw new InvalidOperationException($"NodeContainer must has not more 1 child. Has '{GetChildCount()}' children.");
		if (GetChildCount() == 1)
		{
			_currentStoredNode?.QueueFree();
			_currentStoredNode = GetChildren()[0];
		}
	}

	public Node ChangeStoredNode(Node newStoredNode)
	{
		_currentStoredNode?.QueueFree();
		_currentStoredNode = newStoredNode;
		AddChild(newStoredNode);
		return newStoredNode;
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
