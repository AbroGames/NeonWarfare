using Godot;
using System;
using NeonWarfare;
using NeonWarfare.Scenes.Screen;
using NeonWarfare.Scenes.World;

namespace NeonWarfare.Scenes.Game.ClientGame.MainScenes;

public interface IWorldMainScene
{
	public ClientWorld GetWorld();
	public Hud GetHud();
	public Node2D GetAsNode2D();
}
