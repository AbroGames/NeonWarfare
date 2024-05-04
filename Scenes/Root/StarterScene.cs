
using Godot;
using KludgeBox;

namespace NeonWarfare;

public partial class StarterScene : Node2D
{
	[Export] public PackedScene Scene { get; private set; }
	public override void _Ready()
	{
        CallDeferred(nameof(StartGame));
	}

	private void StartGame()
	{
		GetTree().ChangeSceneToPacked(Scene);
	}
}