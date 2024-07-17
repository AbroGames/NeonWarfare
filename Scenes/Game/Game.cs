using Godot;
using System;
using KludgeBox;
using NeonWarfare;
using NeonWarfare.NetOld;

public abstract partial class Game : Node2D
{

	public WorldMainScene MainScene { get; private set; }
	public NetworkEntityManager NetworkEntityManager { get; private set; } = new();
	
	public override void _Ready()
	{
		NotNullChecker.CheckProperties(this);
	}

	public override void _Process(double delta)
	{
	}

	public void ChangeMainScene(WorldMainScene worldMainScene)
	{
		MainScene?.QueueFree();
		MainScene = worldMainScene;
		AddChild(MainScene);
	}
}
