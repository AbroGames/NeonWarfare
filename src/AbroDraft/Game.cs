using Godot;

namespace AbroDraft;

public partial class Game : Node2D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Maximized);
		var firstScene = References.Instance.FirstSceneBlueprint;
		References.Instance.MenuContainer.ChangeStoredNode(firstScene.Instantiate() as Control);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}