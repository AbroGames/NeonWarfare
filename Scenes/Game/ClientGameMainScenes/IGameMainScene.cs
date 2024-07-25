using Godot;
using System;
using NeonWarfare;

public interface IGameMainScene
{
	public World GetWorld();
	public Hud GetHud();
	public Node2D GetAsNode2D();
}
